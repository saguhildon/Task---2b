using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TaskProcessorAPI.Models
{
    public class TaskModel
    {
        [Key]
        public int TaskID { get; set; }
        public string TaskDescription { get; set; }
        public string TaskPriority { get; set; }
        public string TaskStatus { get; set; }
        public int CustomerID { get; set; }
    }
}
