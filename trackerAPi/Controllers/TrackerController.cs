using Core.DomainModel;
using Core.DomainServices;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using System.Web.Http.Cors;

namespace trackerAPi.Controllers
{
    public class TrackerController : ApiController
    {
        private readonly IGenericRepository<Manga> _mangaRepository;
        private readonly IGenericRepository<Lists> _listRepository;
        private readonly IUnitOfWork _unitOfWork;
        private Lists list;

        public TrackerController()
        {
            var sm = new SampleContext();
            _unitOfWork = new UnitOfWork(sm);
            _mangaRepository = new GenericRepository<Manga>(sm);
            _listRepository = new GenericRepository<Lists>(sm);
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public IOrderedEnumerable<Manga> Get(string sortOrder = "new_chapter_desc")
        {

            list = _listRepository.Get().Single(s => s.Owner == "948395601842199");


            IOrderedEnumerable<Manga> mangas;
            switch (sortOrder)
            {
                case "name_desc":
                    mangas = list.Mangas.OrderByDescending(s => s.Name);
                    break;
                case "new_chapter":
                    mangas = list.Mangas.OrderBy(s => s.NewChapter);
                    break;
                case "new_chapter_desc":
                    mangas = list.Mangas.OrderByDescending(s => s.NewChapter);
                    break;
                default:
                    mangas = list.Mangas.OrderBy(s => s.Name);
                    break;
            }
            return mangas;
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpPost]
        public void CheckForNew()
        {
            list = _listRepository.Get().Single(s => s.Owner == "948395601842199");
            var listofMangas = list.Mangas;
            List<Manga> todaysshows = new List<Manga>();


            foreach (var manga in listofMangas)
            {
                WebRequest request = WebRequest.Create("https://api.mangarockhd.com/query/web400/mrs_quick_search?country=Denmark");
                request.Method = "POST";
                request.ContentLength = manga.Name.Length;
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(Encoding.ASCII.GetBytes(manga.Name), 0, manga.Name.Length);
                }

                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());

                string text = reader.ReadToEnd();

            }
        }
    }
}
