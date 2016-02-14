﻿using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using GamingSessionApp.BusinessLogic;
using GamingSessionApp.Models;
using GamingSessionApp.ViewModels.Profile;
using Microsoft.AspNet.Identity;

namespace GamingSessionApp.Controllers
{
    [Route("Profile/[action]")]
    public class ProfileController : BaseController
    {
        private readonly ProfileLogic _profileLogic;

        public ProfileController(ProfileLogic profileLogic)
        {
            _profileLogic = profileLogic;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("Profile/{userName}")]
        public async Task<ActionResult> UserProfile(string userName)
        {
            if(userName == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            //Is my profile?
            if (userName == User.Identity.GetUserName())
            {
                UserProfileViewModel model = await _profileLogic.GetMyProfile(UserId);

                return View("MyProfile", model);
            }
            else
            {
                UserProfileViewModel model = await _profileLogic.GetUserProfile(userName);

                if (model == null) return new HttpStatusCodeResult(HttpStatusCode.NotFound);

                return View("UserProfile", model);
            }
        }

        [HttpGet]
        [Route("Profile/Edit")]
        public async Task<ActionResult> Edit()
        {
            if (TempData.ContainsKey("ErrorMsg"))
                ModelState.AddModelError("", TempData["ErrorMsg"].ToString());

            var model = await _profileLogic.GetEditProfileModel(UserId);

            if (model == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            return View(model);
        }

        [HttpPost]
        [Route("Profile/Edit")]
        public async Task<ActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ValidationResult result = await _profileLogic.EditProfile(model, UserId);

            if (result.Success)
            {
                return RedirectToAction("UserProfile", new { userName = User.Identity.GetUserName() });
            }

            //Something went wrong
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> AddFriend(string userName)
        {
            if(userName == null)
                return Json(new { success = false, responseText = "No user provided" });

            _profileLogic.UserId = UserId;

            ValidationResult result = await _profileLogic.AddFriend(userName);

            //Return Success
            if (result.Success)
                return Json(new { success = true, responseText = "Friend added" });
            
            //Return error
            return Json(new { success = false, responseText = result.Error });
        }
        

        [ChildActionOnly]
        public PartialViewResult UserMenu()
        {
            var model = _profileLogic.GetUserMenuInformation(UserId);

            return PartialView("_UserMenu", model);
        }

        [HttpGet]
        [Route("Profile/GetUsersJson")]
        public async Task<JsonResult> GetUsersJson(string term)
        {
            var users = await _profileLogic.GetUsersJson(term);

            return Json(users, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("Profile/ImageUpload")]
        public async Task<ActionResult> ImageUpload(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength <= 0)
            {
                TempData["ErrorMsg"] = "No image was selected. Please select an image.";
                return RedirectToAction("Edit");
            }

            ValidationResult result = await _profileLogic.ProcessImageUpload(file, UserId);

            if (result.Success)
            {
                return RedirectToAction("Edit");
            }

            //Something went wrong
            TempData["ErrorMsg"] = result.Error;
            return RedirectToAction("Edit");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
              _profileLogic.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}