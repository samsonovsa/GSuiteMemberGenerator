using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSuite.Libs.Models;
using GSuite.Libs.Services.Interfaces;

namespace GSuite.Libs.Services
{
    public class EntityReader : IEntityReader
    {
        public List<Entity> GetEntityes(string fileName)
        {
            List<Entity> entities = new List<Entity>();

            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    Entity entity = new Entity(line.Trim());
                    if(entity.Validator())
                        entities.Add(entity);
                }
            }

            return entities;
        }
    }
}
