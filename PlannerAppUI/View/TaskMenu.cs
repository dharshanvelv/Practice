using PlannerApplication.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlannerApplication.View
{
    public class TaskMenu
    {
        public TaskService taskService { get; set; }

        public TaskMenu(TaskService? taskservice = null)
        {
            this.taskService = taskservice ?? new TaskService();
        }


    }
}
