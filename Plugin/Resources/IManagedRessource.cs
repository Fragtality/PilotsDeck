using PilotsDeck.Resources.Images;
using PilotsDeck.Resources.Scripts;
using PilotsDeck.Resources.Variables;
using System;

namespace PilotsDeck.Resources
{
    public interface IManagedRessource : IDisposable
    {
        public string UUID { get; }
        public int Registrations { get; set; }
        
        public virtual void AddRegistration()
        {
            Registrations++;
        }

        public virtual void RemoveRegistration()
        {
            if (Registrations > 0)
                Registrations--;
        }
    }

    public static class RessourceExtensions
    {
        public static void AddRegistration(this ManagedImage ressource)
        {
            (ressource as IManagedRessource)?.AddRegistration();
        }

        public static void RemoveRegistration(this ManagedImage ressource)
        {
            (ressource as IManagedRessource)?.RemoveRegistration();
        }

        public static void AddRegistration(this ManagedVariable ressource)
        {
            (ressource as IManagedRessource)?.AddRegistration();
        }

        public static void RemoveRegistration(this ManagedVariable ressource)
        {
            (ressource as IManagedRessource)?.RemoveRegistration();
        }

        public static void AddRegistration(this ManagedScript ressource)
        {
            (ressource as IManagedRessource)?.AddRegistration();
        }

        public static void RemoveRegistration(this ManagedScript ressource)
        {
            (ressource as IManagedRessource)?.RemoveRegistration();
        }
    }
}
