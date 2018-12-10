using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSuite.Libs.Helpers
{
    class AddingMembersStateEventArgs
    {
        public int NumberAddingMemberInGroup { get; set; }
        public string GroupName { get; set; }

        public AddingMembersStateEventArgs(string group, int numMember)
        {
            GroupName = group;
            NumberAddingMemberInGroup = numMember;
        }
    }
}
