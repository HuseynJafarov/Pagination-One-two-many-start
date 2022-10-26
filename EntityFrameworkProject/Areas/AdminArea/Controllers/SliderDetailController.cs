using EntityFrameworkProject.Data;
using EntityFrameworkProject.Helpers;
using EntityFrameworkProject.Models;
using EntityFrameworkProject.ViewModels.SliderDetailVM;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFrameworkProject.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    public class SliderDetailController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SliderDetailController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: SliderDetailController
        public async Task<IActionResult> Index()
        {
            SliderDetail sliderDetail = await _context.SliderDetails.Where(m => !m.IsDeleted).FirstOrDefaultAsync();

            ViewBag.count = await _context.SliderDetails.Where(m => !m.IsDeleted).CountAsync();

            return View(sliderDetail);
        }

        // GET: SliderDetailController/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return BadRequest();
            SliderDetail sliderDetail = await _context.SliderDetails.FindAsync(id);
            if (sliderDetail == null)
            {
                return NotFound();
            }
            return View(sliderDetail);
        }

        // GET: SliderDetailController/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: SliderDetailController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SliderDetailCreateVM sliderDetail)
        {
            if (!ModelState.IsValid) return View();
            
            try
            {
                foreach (var singPhoto in sliderDetail.SingPhotos)
                {
                    if (!singPhoto.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "Please choose correct image type");
                        return View();
                    }
                    if (!singPhoto.CheckFileSize(2000))
                    {
                        ModelState.AddModelError("Photo", "Please choose correct image size");
                        return View();
                    }
                    
                }

                foreach (var singPhoto in sliderDetail.SingPhotos)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + singPhoto.FileName;

                    string path = Helper.GetFilePath(_env.WebRootPath, "img", fileName);

                    await SaveFile(path, singPhoto);

                    SliderDetail newSliderDetail = new SliderDetail
                    {
                        Photo = fileName,
                        Header = sliderDetail.Header,
                        Description =sliderDetail.Desc
                        
                    };

                    await _context.SliderDetails.AddAsync(newSliderDetail);
                }

                
                    bool isExist = await _context.SliderDetails.AnyAsync(m => m.Header.Trim() == sliderDetail.Header.Trim() && 
                    m.Description.Trim() == sliderDetail.Desc.Trim());
                    if (isExist)
                    {
                        ModelState.AddModelError("Header Description", "Category already exist");
                        return View();
    

                    }


                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id is null) return BadRequest();

                SliderDetail sliderDetail = await _context.SliderDetails.FirstOrDefaultAsync(m => m.Id == id);

                if (sliderDetail is null) return NotFound();

                return View(sliderDetail);

            }
            catch (Exception ex)
            {

                ViewBag.Message = ex.Message;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SliderDetail sliderDetail)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(sliderDetail);
                }

                if (sliderDetail.SignImage != null)
                {
                    if (!sliderDetail.SignImage.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "Please choose correct image type");
                        return View();
                    }

                    if (!sliderDetail.SignImage.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photo", "Please choose correct image size");
                        return View();
                    }

                    string fileName = Guid.NewGuid().ToString() + "_" + sliderDetail.SignImage.FileName;

                    SliderDetail dbSliderDetail = await _context.SliderDetails.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

                    if (dbSliderDetail is null) return NotFound();

                    if (dbSliderDetail.Header.Trim().ToLower() == sliderDetail.Header.Trim().ToLower()
                        && dbSliderDetail.Description.Trim().ToLower() == sliderDetail.Description.Trim().ToLower()
                        && dbSliderDetail.SignImage == sliderDetail.SignImage)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    string path = Helper.GetFilePath(_env.WebRootPath, "img", fileName);

                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        await sliderDetail.SignImage.CopyToAsync(stream);
                    }

                    sliderDetail.Photo = fileName;

                    _context.SliderDetails.Update(sliderDetail);

                    await _context.SaveChangesAsync();

                    string dbPath = Helper.GetFilePath(_env.WebRootPath, "img", dbSliderDetail.Photo);

                    Helper.DeleteFile(dbPath);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View();
            }
        }

        // GET: SliderDetailController/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            SliderDetail sliderDetail = await GetByIdAsync(id);

            if (sliderDetail == null) return NotFound();

            string path = Helper.GetFilePath(_env.WebRootPath, "img", sliderDetail.Photo);

            Helper.DeleteFile(path);

            _context.SliderDetails.Remove(sliderDetail);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private async Task SaveFile(string path, IFormFile Singphoto)
        {
            using FileStream stream = new FileStream(path, FileMode.Create);
            await Singphoto.CopyToAsync(stream);
        }

        private async Task<SliderDetail> GetByIdAsync(int id)
        {
            return await _context.SliderDetails.FindAsync(id);
        }
    }
}
