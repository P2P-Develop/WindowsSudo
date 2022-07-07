using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;

namespace WindowsSudo
{
    public static class CredentialHelper
    {
        private static readonly HashSet<string> domains_cache = new HashSet<string>();

        public static bool DomainExists(string domain)
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
            try
            {
                NTAccount acct = new NTAccount(username);
                SecurityIdentifier id = (SecurityIdentifier)acct.Translate(typeof(SecurityIdentifier));

                return id.IsAccountSid();
            }
            catch (IdentityNotMappedException)
            {
                return false;
            }
        }

        public static bool UserExists(string username, string domain)
        {
            if (domain == null)
                return UserExists(username);

            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domain))
            {
                return UserPrincipal.FindByIdentity(context, username) != null;
            }
        }

        [DllImport("advapi32.dll")]
        public static extern bool LogonUser(string userName, string domainName, string password, int LogonType,
            int LogonProvider, ref IntPtr phToken);


        public static bool ValidateAccount(string name, string password)
        {
            if (password == null &&
                UserExists(name) &&
                !UserPasswordRequired(name))
                return true;


            IntPtr tokenHandler = IntPtr.Zero;
            return LogonUser(name, Dns.GetHostName(), password, 2, 0, ref tokenHandler);
        }

        public static bool ValidateAccount(string name, string password, string domain, bool exception = false)
        {
            if (domain != null && !ACAvailable())
                if (exception)
                    throw new Exception("Domain name specified but Active Directory is not available in this machine.");
                else
                    return false;

            if (domain != null && !DomainExists(domain))
                if (exception)
                    throw new Exceptions.DomainNotFoundException("Could not find domain " + domain);
                else
                    return false;

            if (!UserExists(name, domain))
                if (exception)
                    throw new Exceptions.UserNotFoundException("Could not find user " + domain);
                else
                    return false;


            if (password == null)
            {
                if (UserPasswordRequired(name, domain))
                    if (exception)
                        throw new Exceptions.PasswordRequiredException("Password required for user " + name);
                    else
                        return false;

                return true;
            }

            if (domain == null)
                return ValidateAccount(name, password);

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

        public static bool UserPasswordRequired(string username, string domain)
        {
            if (domain == null)
                return UserPasswordRequired(username);

            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domain))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, username);

                    if (user == null)
                        return true;
                    user.ChangePassword("", "");
                    return false;
                }
            }
            catch (PasswordException)
            {
                return true;
            }
            catch (PrincipalOperationException)
            {
                return true;
            }
        }

        public static bool UserPasswordRequired(string username)
        {
            IntPtr tokenHandler = IntPtr.Zero;
            return !LogonUser(username, "", "", 2, 0, ref tokenHandler);
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
            [Serializable]
            public class UserNotFoundException : Exception
            {
                public UserNotFoundException(string message) : base(message)
                {
                }

                public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected UserNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
                {
                }
            }

            [Serializable]
            public class BadPasswordException : Exception
            {
                public BadPasswordException(string message) : base(message)
                {
                }

                public BadPasswordException(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected BadPasswordException(SerializationInfo info, StreamingContext context) : base(info, context)
                {
                }
            }

            [Serializable]
            public class PasswordRequiredException : Exception
            {
                public PasswordRequiredException(string message) : base(message)
                {
                }

                public PasswordRequiredException(string message, Exception innerException) : base(message,
                    innerException)
                {
                }

                protected PasswordRequiredException(SerializationInfo info, StreamingContext context) : base(info,
                    context)
                {
                }
            }


            [Serializable]
            public class DomainNotFoundException : Exception
            {
                public DomainNotFoundException(string message) : base(message)
                {
                }

                public DomainNotFoundException(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected DomainNotFoundException(SerializationInfo info, StreamingContext context) : base(info,
                    context)
                {
                }
            }

            [Serializable]
            public class ActiveDirectoryNotAvailable : Exception
            {
                public ActiveDirectoryNotAvailable(string message) : base(message)
                {
                }

                public ActiveDirectoryNotAvailable(string message, Exception innerException) : base(message,
                    innerException)
                {
                }

                protected ActiveDirectoryNotAvailable(SerializationInfo info, StreamingContext context) : base(info,
                    context)
                {
                }
            }
        }
    }
}
