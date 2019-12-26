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
            List<Manga> mangaRockErrors = new List<Manga>();
            List<Manga> manganeloErrors = new List<Manga>();

            foreach (var manga in listofMangas)
            {
                try
                {
                    tryMangaRock(manga);
                }
                catch (Exception e)
                {
                    mangaRockErrors.Add(manga);
                    System.Diagnostics.Debug.WriteLine(manga.Name + " " + manga.MangaRockId);
                }
                try
                {
                    tryManganelo(manga);
                }
                catch (Exception e)
                {
                    manganeloErrors.Add(manga);
                    System.Diagnostics.Debug.WriteLine(manga.Name + " " + manga.MangaRockId);
                }
            }

            IEnumerable<Manga> commonErros = manganeloErrors.Intersect(mangaRockErrors,new MangaComparer());

            return listofMangas.OrderByDescending(s => s.NewChapter);
        }


        private bool wrongMangaRockId(string id){
            WebRequest request = WebRequest.Create("https://mangarock.com/manga/" + id);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            string text = reader.ReadToEnd();

            return text.IndexOf("Page Not Found") != -1;
        }

        private void tryMangaRock(Manga manga)
        {
            if (String.IsNullOrEmpty(manga.MangaRockId) || wrongMangaRockId(manga.MangaRockId))
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
                    return;
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

        private bool wrongManganeloId(string id)
        {
            WebRequest request = WebRequest.Create("https://manganelo.com/manga/" + id);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            string text = reader.ReadToEnd();

            return text.IndexOf("Sorry, the page you have requested cannot be found.") != -1;
        }

        private void tryManganelo(Manga manga)
        {
         
            if (String.IsNullOrEmpty(manga.ManganeloId) || wrongManganeloId(manga.ManganeloId))
            {
                WebRequest searchRequest = WebRequest.Create("https://manganelo.com/search/"+ manga.Name.Replace(' ','_'));
                WebResponse searchResponse = searchRequest.GetResponse();
                StreamReader searchReader = new StreamReader(searchResponse.GetResponseStream());

                string searchText = searchReader.ReadToEnd();

                var indexOfName = searchText.IndexOf("\">"+manga.Name);

                var indexOfUrl = searchText.IndexOf("https://manganelo.com/manga/", indexOfName - "https://manganelo.com/manga/".Length - 40);

                string serie = searchText.Substring(indexOfUrl+ "https://manganelo.com/manga/".Length, indexOfName- (indexOfUrl+ "https://manganelo.com/manga/".Length));

                manga.ManganeloId = serie;
                _mangaRepository.Update(manga);
                _unitOfWork.Save();
            }


            WebRequest request = WebRequest.Create("https://manganelo.com/manga/" + manga.ManganeloId);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            string text = reader.ReadToEnd().ToLower();

            var indexOfChapter = text.IndexOf("href=\"https://manganelo.com/chapter/" + manga.ManganeloId + "/chapter_") + 45 + manga.ManganeloId.Length;

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

    class MangaComparer : IEqualityComparer<Manga>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Manga x, Manga y)
        {

            return x.MangaId == y.MangaId;
        }

        // If Equals() returns true for a pair of objects 
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Manga manga)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(manga, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashProductName = manga.Name == null ? 0 : manga.Name.GetHashCode();

            //Get hash code for the Code field.
            int hashProductCode = manga.MangaId.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName ^ hashProductCode;
        }

    }
}
