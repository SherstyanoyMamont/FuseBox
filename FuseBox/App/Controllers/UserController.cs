using System.Collections.Generic;

namespace FuseBox.App.Controllers
{
    public class UserController
    {
        public Guid UserId { get; private set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string PasswordHash { get; private set; }
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<string> ActivityLog { get; set; } = new List<string>();
        public DateTime CreatedAt { get; private set; }
        public DateTime LastLogin { get; private set; }

        public UserController(string name, string email, int age, string address)
        {
            UserId = Guid.NewGuid();
            Name = name;
            Email = email;
            Address = address;
            CreatedAt = DateTime.Now;
            LastLogin = DateTime.Now;
        }

        public void SetPassword(string password)
        {
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        public bool ValidatePassword(string password)
        {
            return PasswordHash == Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        public void LogActivity(string activity)
        {
            ActivityLog.Add($"{DateTime.Now}: {activity}");
            Console.WriteLine($"Activity logged: {activity}");
        }
        public void Login()
        {
            LastLogin = DateTime.Now;
            LogActivity("User logged in.");
        }

        public void AddProject(Project project)
        {
            Projects.Add(project);
            LogActivity($"Project '{project.Name}' added.");
        }
    }
}
