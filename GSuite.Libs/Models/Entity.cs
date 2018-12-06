using System;
using System.Text.RegularExpressions;

namespace GSuite.Libs.Models
{
    public  class Entity
    {
        public string Name { get; set; }

        public Entity(string name)
        {
            Name = name;
        }

        public virtual bool Validator()
        {
            return IsValidName(Name);
        }

        private bool IsValidName(string name)
        {
            // source: http://thedailywtf.com/Articles/Validating_Email_Addresses.aspx
            Regex rx = new Regex(
            @"^[-!#$%&'*+/0-9=?A-Z^_a-z{|}~](\.?[-!#$%&'*+/0-9=?A-Z^_a-z{|}~])");
            return rx.IsMatch(name);
        }


    }
}
