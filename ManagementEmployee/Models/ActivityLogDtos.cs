using System;

namespace ManagementEmployee.Models
{
    public class ActivityLogDto
    {
        public int LogId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public string UserDisplayName { get; set; } = "";
        public string Action { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string Details { get; set; } = "";
    }

    public class UserLookupItem
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = "";
    }
}
