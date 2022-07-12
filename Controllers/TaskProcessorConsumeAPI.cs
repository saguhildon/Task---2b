using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskProcessorAPI.Models;

public class TaskProcessorConsumeAPI : BackgroundService, IDisposable
    {
    private readonly IServiceProvider _sp;
    private readonly TaskModelContext _context;
    private readonly ILogger _logger;
    private IConnection _connection;
    private IModel _channel;

    public TaskProcessorConsumeAPI(ILoggerFactory loggerFactory,IServiceScopeFactory factory)
    {
        this._logger = loggerFactory.CreateLogger<TaskProcessorConsumeAPI>();
        _context = factory.CreateScope().ServiceProvider.GetRequiredService<TaskModelContext>();
        InitRabbitMQ();
    }

    private void InitRabbitMQ()
    {
        var factory = new ConnectionFactory
        {

            //HostName = "localhost",
            //Port = 31672
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
            Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))

        };

        // create connection  
        _connection = factory.CreateConnection();

        // create channel  
        _channel = _connection.CreateModel();

        //_channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);
        _channel.QueueDeclare("tasks", false, false, false, null);
        // _channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);
        // _channel.BasicQos(0, 1, false);

        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            // received message  
            var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

            // handle the received message  
            HandleMessage(content);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        consumer.Shutdown += OnConsumerShutdown;
        consumer.Registered += OnConsumerRegistered;
        consumer.Unregistered += OnConsumerUnregistered;
        consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

        _channel.BasicConsume("tasks", false, consumer);
        return Task.CompletedTask;
    }

    private void HandleMessage(string content)
    {
        
         Save_to_Model(content);
        // we just print this message   
        _logger.LogInformation($"consumer received {content}");
    }

    private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
    private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }

    public void Save_to_Model(string json)
    {
        TaskModel taskmodel = JsonConvert.DeserializeObject<TaskModel>(json);
        taskmodel.TaskStatus = "IN_PROGRESS";
        int taskid = taskmodel.TaskID;
        Console.WriteLine("TaskID" + taskid.ToString());
        _context.AddRange(taskmodel);
        _context.SaveChanges();
        taskmodel.TaskStatus = "COMPLETED";

        //Console.WriteLine("TaskID" + taskid.ToString());
        _context.Entry(taskmodel).State = EntityState.Modified;
        _context.SaveChangesAsync();
        string jsonData = JsonConvert.SerializeObject(taskmodel);
        publishmessages(jsonData);

    }

    public void publishmessages(string json)
    {
        var factory = new ConnectionFactory()
        {
            //HostName = "localhost",
            //Port = 31672
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
            Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))

        };

        Console.WriteLine(factory.HostName + ":" + factory.Port);
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "task-processed",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "",
                                 routingKey: "task-processed",
                                 basicProperties: null,
                                 body: body);
        }
    }
}