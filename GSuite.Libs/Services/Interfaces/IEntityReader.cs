
using System.Collections.Generic;
using GSuite.Libs.Models;

namespace GSuite.Libs.Services.Interfaces
{
    public  interface IEntityReader
    {
        List<Entity> GetEntityes(string fileName);
    }
}
