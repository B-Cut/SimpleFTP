using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP.Server
{
    /// <summary>
    /// This class deals with the information of the current user and the state of their account
    /// </summary>
    internal class User
    {
        private string account;
        private string userid;
        private bool providedPassword;
        private readonly bool useAccountAndPassword;
        private bool logged;

        public string UserId { get { return userid; } }

        public User(bool useAccountAndPassword = true)
        {
            account = "";
            userid = "";
            logged = false;
            providedPassword = false;
            this.useAccountAndPassword = useAccountAndPassword;
        }

        // TODO: Proper user account checking

        /// <summary>
        /// Verifies if user is already logged in.
        /// </summary>
        /// <returns></returns>
        public bool IsUserLogged()
        {
            if (!useAccountAndPassword)
            {
                return HasValidUserId();
            }
            // The user can provide their login info in about any order, so doing this guarantees we check if all conditions are met
            if (!logged)
            {
                if (HasValidPassword() && HasValidUserId() && HasValidAccount())
                {
                    logged = true;
                }
            }

            return logged;
        }
        /// <summary>
        /// Verifies if user has already privided a account
        /// </summary>
        /// <returns></returns>
        public bool HasValidAccount()
        {
            return account != "";
        }
        /// <summary>
        /// Verifies if user has already provided a userId
        /// </summary>
        /// <returns></returns>
        public bool HasValidUserId()
        {
            return userid != "";
        }
        /// <summary>
        /// Verifies if user has already provided a password
        /// </summary>
        /// <returns></returns>
        public bool HasValidPassword()
        {
            return providedPassword;
        }
        // For now, we don't care about account validation, so we just return true to anything

        /// <summary>
        /// Validates the user-id provided. If user-id is valid, <c>User</c> uses it as it's current password
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool ValidateUserId(string userid)
        {
            if (this.userid == "")
            {
                this.userid = userid.Trim();
            }
            return true;
        }

        /// <summary>
        /// Validates the account provided. If account is valid, <c>User</c> uses it as it's current account
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool ValidateAccount(string account)
        {
            if(this.account == "")
            {
                this.account = account.Trim();
            }
            return true;
        }
        /// <summary>
        /// Validates the password provided. If password is valid, <c>User</c> uses it as it's current password
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool ValidatePassword(string password)
        {      
            providedPassword = true;
            return true;
        }
    }
}
