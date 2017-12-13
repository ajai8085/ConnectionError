using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadTester
{

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class IgnoreDataMemberAttribute:Attribute
    {
    }


    public interface IEntityWithId
    {
        int Id { get; set; }
    }
}
