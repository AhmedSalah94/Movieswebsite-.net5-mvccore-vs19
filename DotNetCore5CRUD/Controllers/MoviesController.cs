
using DotNetCore5CRUD.Models;
using DotNetCore5CRUD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore5CRUD.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        private new List<string> _allowedExtenstions = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }


        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.OrderByDescending(m => m.Rate).ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };

            return View("MovieForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();//هملا الدروب داون ليست بكل الجنراز اللى عندى
                return View("MovieForm", model);
            }

            //هعمل server side valydations

            //check if pster sent or not
            var files = Request.Form.Files;//هتجيب كل الفايلات(الصور هنا)اللي بعتها اليوزر مع الفورم

            if (!files.Any())//لو اليوزر مبعتش ولا فايل مع الفورم لما داس سابميت
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();//هملا الدروب داون ليست بكل الجنراز اللى عندى
                ModelState.AddModelError("Poster", "Please select movie poster!");//هكريت كى وفاليو للايرور
                return View("MovieForm", model); //MovieForm هنا هنروح لفيو مش بنفس اسم الاكشن يعنى مش كريت فلازم نكتب اسمه كاول بارميتر والبارميتر التانى ده المودل اللى هيتبعت عادى >>ده اسم فيو 
            }

            //check poster extensions
            var poster = files.FirstOrDefault();//البوستر هنا استحالة تكون بنل لاننا شيكنا فى الخطوه اللى فاتت اتها مش فاضية

            if (!_allowedExtenstions.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .PNG, .JPG images are allowed!");
                return View("MovieForm", model);
            }

            //check poster size
            if (poster.Length > _maxAllowedPosterSize)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
                return View("MovieForm", model);
            }

            //بعد ماعملنا الفاليداشنز هنسيف الريكورد الجديد ده فى الداتا بيز

            using var dataStream = new MemoryStream();

            await poster.CopyToAsync(dataStream);//حول البوستر لداتا ستريم هشان هيتخزن فى الميمورى فى شكل اراى اوف بايت

            var movies = new Movie  // هعمل ماب للفاليوز اللى جايالي فى الفيو مودل للمودل اللى هيتبعت للداتابيز
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                Storeline = model.Storeline,
                Poster = dataStream.ToArray()
            };

            _context.Movies.Add(movies);
            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Movie created successfully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            var viewModel = new MovieFormViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                GenreId = movie.GenreId,
                Rate = movie.Rate,
                Year = movie.Year,
                Storeline = movie.Storeline,
                Poster = movie.Poster,
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };

            return View("MovieForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }

            var movie = await _context.Movies.FindAsync(model.Id);

            if (movie == null)
                return NotFound();

            var files = Request.Form.Files; //لو اليوزر لما عدل بعتلك فايلات -صورة جديدة- حطها هنا عشان اشيك عليها ولو تمام احطها فى الداتابيز مكان القديمة)

            if (files.Any())
            {
                var poster = files.FirstOrDefault(); 

                using var dataStream = new MemoryStream();

                await poster.CopyToAsync(dataStream); //لو اليوزر بعت صورة جديدة هحولها لداتا ستريم عشان اخزنها فى الداتابيز

                model.Poster = dataStream.ToArray();

                if (!_allowedExtenstions.Contains(Path.GetExtension(poster.FileName).ToLower())) //هشيك على امتدادالصورة الجديدة
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .PNG, .JPG images are allowed!");
                    return View("MovieForm", model);
                }

                if (poster.Length > _maxAllowedPosterSize) //هشيك على حجم الصورة الجديدة
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
                    return View("MovieForm", model);
                }

                movie.Poster = model.Poster; //شيكنا وتمام عالصوره الجديدة فهسيفها فى الداتا بيز
            }

            movie.Title = model.Title; //هسيف باقى القيم اللة دخلها اليوزر فى الداتا بيز -اكيد مش هسيف الصورة بس يعنى-ا
            movie.GenreId = model.GenreId;
            movie.Year = model.Year;
            movie.Rate = model.Rate;
            movie.Storeline = model.Storeline;

            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Movie updated successfully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.Include(m => m.Genre).SingleOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            _context.Movies.Remove(movie);
            _context.SaveChanges();

            return Ok(); 
            //الستاتس كود 200 دي بتاعة اوك هتنده عالميثود ساكسيس اللى موجودة جوة الاجاكس
        }
    }
}