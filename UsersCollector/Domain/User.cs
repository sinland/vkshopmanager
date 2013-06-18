using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UsersCollector.Domain
{
    public class User
    {
        public virtual int Id { get; set; }
        public virtual Int64 VkId { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int Sex { get; set; }
        public virtual string MobilePhone { get; set; }
        public virtual string HomePhone { get; set; }
        public virtual int BirthYear { get; set; }

        public User()
        {
            
        }
    }
}
