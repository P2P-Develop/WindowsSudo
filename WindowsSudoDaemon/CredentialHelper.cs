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
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                return UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username) != null;
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

        public static bool ValidateAccount(string name, string password)
        {
            if (password == null &&
                UserExists(name) &&
                !UserPasswordRequired(name))
                return true;

            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
                {
                    return pc.ValidateCredentials(name, password);
                }
            }
            catch (PrincipalOperationException)
            {
                return false;
            }
        }

        public static bool ValidateAccount(string name, string password, string domain, bool exception=false)
        {
            if (password == null)
            {
                if (!DomainExists(domain))
                    if (exception)
                        throw new Exceptions.DomainNotFoundException("Could not find domain " + domain);
                    else
                        return false;

                if (!UserExists(name, domain))
                    if (exception)
                        throw new Exceptions.UserNotFoundException("Could not find user " + domain);
                    else
                        return false;

                if (UserPasswordRequired(name, domain))
                    if (exception)
                        throw new Exceptions.PasswordRequiredException("Password required for user " + name);
                    else
                        return false;

                return true;
            }
            else if (!UserExists(name, domain))
                if (exception)
                    throw new Exceptions.UserNotFoundException("Could not find user " + name);
                else
                    return false;

            if (domain == null)
                ValidateAccount(name, password);

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
                    else
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
            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, username);

                    if (null == user)
                        return true;
                    else
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
            public class PasswordRequiredException : Exception
            {
                public PasswordRequiredException(string message) : base(message) { }
                public PasswordRequiredException(string message, Exception innerException) : base(message, innerException) { }
                protected PasswordRequiredException(SerializationInfo info, StreamingContext context) : base(info, context) { }
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
