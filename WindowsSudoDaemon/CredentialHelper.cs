using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.Serialization;

namespace WindowsSudo
{
    public static class CredentialHelper
    {
        private static readonly HashSet<string> domains_cache = new HashSet<string>();

        public static bool DomainExist(string domain)
        {
            if (domains_cache.Count != Forest.GetCurrentForest().Domains.Count)
            {
                domains_cache.Clear();
                foreach (Domain d in Forest.GetCurrentForest().Domains)
                    domains_cache.Add(d.Name.ToLower());
            }

            return domains_cache.Contains(domain);
        }

        public static bool UserExists(string username)
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                return UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username) != null;
            }
        }

        public static bool UserExists(string username, string domain)
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domain))
            {
                return UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username) != null;
            }
        }

        public static bool ValidateAccount(string name, string password)
        {
            ContextType ct = ContextType.Machine;

            try
            {
                using (PrincipalContext pc = new PrincipalContext(ct))
                {
                    return pc.ValidateCredentials(name, password);
                }
            }
            catch (PrincipalOperationException)
            {
                return false;
            }
        }

        public static bool ValidateAccount(string name, string password, string domain)
        {
            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain))
                {
                    return pc.ValidateCredentials(name, password);
                }
            }
            catch (PrincipalOperationException)
            {
                return false;
            }
        }

        public static bool ACAvailable()
        {
            try
            {
                Domain.GetComputerDomain();
                return true;
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                return false;
            }
        }

        public static class Exceptions
        {
            [Serializable()]
            public class UserNotFoundException : Exception
            {
                public UserNotFoundException(string message) : base(message) { }
                public UserNotFoundException(string message, Exception innerException) : base(message, innerException) { }
                protected UserNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
            }

            [Serializable()]
            public class BadPasswordException : Exception
            {
                public BadPasswordException(string message) : base(message) { }
                public BadPasswordException(string message, Exception innerException) : base(message, innerException) { }
                protected BadPasswordException(SerializationInfo info, StreamingContext context) : base(info, context) { }
            }


            [Serializable()]
            public class DomainNotFoundException : Exception
            {
                public DomainNotFoundException(string message) : base(message) { }
                public DomainNotFoundException(string message, Exception innerException) : base(message, innerException) { }
                protected DomainNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
            }
        }
    }
}
