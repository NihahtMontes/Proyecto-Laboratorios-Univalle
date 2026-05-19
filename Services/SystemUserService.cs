namespace Proyecto_Laboratorios_Univalle.Services
{
    public class SystemUserService : ICurrentUserService
    {
        public int? UserId => 1; // ID del usuario administrador creado en el Seed
    }
}
