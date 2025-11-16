using System;

namespace SmartPOS.UI.Models
{
    public class CashierModel
    {
        /// <summary>
        /// Full real name of the cashier.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unique username used for login.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password (never store plain text password).
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Cashier role in case you add Managers, Supervisors, etc.
        /// </summary>
        public string Role { get; set; } = "Cashier";

        /// <summary>
        /// Whether the cashier is allowed to log in.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Track when the cashier joined the system.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last time the cashier logged in.
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Terminal or workstation assigned for auditing.
        /// </summary>
        public string TerminalId { get; set; } = "POS-1";

        /// <summary>
        /// Store/branch ID for multi-branch supermarkets.
        /// </summary>
        public string StoreId { get; set; } = "MAIN";
    }
    
}
