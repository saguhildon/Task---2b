using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskProcessorAPI.Models;

namespace TaskProcessorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskModelsController : ControllerBase
    {
        private readonly TaskModelContext _context;

        public TaskModelsController(TaskModelContext context)
        {
            _context = context;
        }

        // GET: api/TaskModels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskModel>>> GetTaskItems()
        {
            return await _context.TaskItems.ToListAsync();
        }

        // GET: api/TaskModels/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskModel>> GetTaskModel(int id)
        {
            var taskModel = await _context.TaskItems.FindAsync(id);

            if (taskModel == null)
            {
                return NotFound();
            }

            return taskModel;
        }

        // PUT: api/TaskModels/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaskModel(int id, TaskModel taskModel)
        {
            if (id != taskModel.TaskID)
            {
                return BadRequest();
            }

            _context.Entry(taskModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TaskModels
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TaskModel>> PostTaskModel(TaskModel taskModel)
        {
            _context.TaskItems.Add(taskModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTaskModel", new { id = taskModel.TaskID }, taskModel);
        }

        // DELETE: api/TaskModels/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskModel(int id)
        {
            var taskModel = await _context.TaskItems.FindAsync(id);
            if (taskModel == null)
            {
                return NotFound();
            }

            _context.TaskItems.Remove(taskModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskModelExists(int id)
        {
            return _context.TaskItems.Any(e => e.TaskID == id);
        }
    }
}
