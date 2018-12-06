using System;
using System.Text.RegularExpressions;

namespace GSuite.Libs.Models
{
    class User: Entity
    {
        public string GroupName { get; set; }

        public User():base("")
        {

        }

        public User(string name):base(name)
        {

        }

        public override bool Validator()
        {
            return IsValidEmail(Name);
        }

        private bool IsValidEmail(string email)
        {
            // source: http://thedailywtf.com/Articles/Validating_Email_Addresses.aspx
            Regex rx = new Regex(
            @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*"
            + "@"
            + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
            return rx.IsMatch(email);
        }


    }
}
