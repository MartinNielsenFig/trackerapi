using Core.DomainModel;
using Core.DomainServices;
using Infrastructure.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        [Route("api/Tracker/CheckForNew")]
        public IOrderedEnumerable<Manga> CheckForNew()
        {
            list = _listRepository.Get().Single(s => s.Owner == "948395601842199");
            var listofMangas = list.Mangas;
            List<Manga> todaysshows = new List<Manga>();


            foreach (var manga in listofMangas)
            {
                try
                {
                    if(manga.MangaRockId == "mrs-serie-64995")
                    {
                        var x = "test";
                    }

                                        if (String.IsNullOrEmpty(manga.MangaRockId))
                    {
                        WebRequest searchRequest = WebRequest.Create("https://api.mangarockhd.com/query/web400/mrs_quick_search?country=Denmark");
                        searchRequest.Method = "POST";
                        searchRequest.ContentLength = manga.Name.Length;
                        using (var dataStream = searchRequest.GetRequestStream())
                        {
                            dataStream.Write(Encoding.ASCII.GetBytes(manga.Name), 0, manga.Name.Length);
                        }

                        WebResponse searchResponse = searchRequest.GetResponse();
                        StreamReader searchReader = new StreamReader(searchResponse.GetResponseStream());

                        string jsonSerieObj = searchReader.ReadToEnd();

                        dynamic obj = JsonConvert.DeserializeObject(jsonSerieObj);

                        if (obj.data.series == null)
                        {
                            continue;
                        }

                        var serie = obj.data.series.ToObject<List<string>>();

                        manga.MangaRockId = serie[0];
                        _mangaRepository.Update(manga);
                        _unitOfWork.Save();
                    }

                    WebRequest request = WebRequest.Create("https://mangarock.com/manga/" + manga.MangaRockId);
                    WebResponse response = request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    string text = reader.ReadToEnd();

                    var indexOfTable = text.LastIndexOf("<tbody");

                    var indexOfChapter = text.IndexOf("Chapter", indexOfTable) + 7;

                    var indexOfNoneNumber = 0;
                    for (int i = indexOfChapter; i < indexOfChapter + 10; i++)
                    {
                        var toCompare = "" + text[i];
                        if (toCompare != " " && !Regex.IsMatch(toCompare, @"^\d+$"))
                        {
                            indexOfNoneNumber = i;
                            break;
                        }
                    }

                    var substring = text.Substring(indexOfChapter, indexOfNoneNumber - indexOfChapter);

                    if (Convert.ToInt32(substring) > manga.Chapter)
                    {
                        manga.NewChapter = true;
                        _mangaRepository.Update(manga);
                        _unitOfWork.Save();
                    }
                }
                catch (Exception)
                {
                    var x = 10;
                }
            }

            return listofMangas.OrderByDescending(s => s.NewChapter);
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpPost]
        [Route("api/Tracker/AddOne")]
        public void AddOne(int id)
        {
            list = _listRepository.Get().Single(s => s.Owner == "948395601842199");
            list.Mangas.Single(s => s.MangaId == id).Chapter++;
            list.Mangas.Single(s => s.MangaId == id).NewChapter = false;
            _mangaRepository.Update(list.Mangas.Single(s => s.MangaId == id));
            _unitOfWork.Save();
        }
    }
}
