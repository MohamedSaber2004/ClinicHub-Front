namespace ClinicHub.Data
{
    public class CurrentUserContext
    {
        public int Id { get; set; }
        public UserRole Role { get; set; }
        public Permission Permissions { get; set; }

        public bool Has(Permission permission) => (Permissions & permission) == permission;

        public static CurrentUserContext? Current { get; set; }
    }
}
