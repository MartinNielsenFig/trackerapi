using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DomainModel
{
    public class Manga
    {
        public int MangaId { get; set; }
        public string Name { get; set; }
        public int Chapter { get; set; }
        public StatusEnum Status { get; set; }
        public int Rating { get; set; }
        public bool NewChapter { get; set; }
        public string MangaRockId { get; set; }
    }
}
