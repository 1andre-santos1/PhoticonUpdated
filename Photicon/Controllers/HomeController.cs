﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Photicon.Models;

namespace Photicon.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private PhoticonDB db = new PhoticonDB();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult MainPage(string Id)
        {
            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");
            Users user = new Users();
            user = db.Users.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();
            user.PicturesList = db.Pictures.Where(m => m.UserFK == user.Id).Select(m => m).ToList();
            return View(user);
        }

        public ActionResult AddPicture(string Id)
        {
            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");
            Users user = new Users();
            user = db.Users.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();
            return View(user);
        }

        [HttpPost]
        public ActionResult AddPicture(HttpPostedFileBase file, string Id, string PictureDescription, bool Visibility, bool IsProfilePicture)
        {

            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");

            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    int picId = db.Pictures.Count() + 1;
                    string path = Path.Combine(Server.MapPath("~/UserPictures"), Path.GetFileName("picture" + picId + "_" + file.FileName));
                    file.SaveAs(path);

                    Users user = db.Users.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();
                    Pictures pic = new Pictures();
                    pic.User = user;
                    pic.CommentariesList = new List<string>();
                    pic.Description = PictureDescription;
                    pic.Link = "";
                    pic.Path = path.Substring(path.IndexOf("UserPictures") - 1);
                    pic.TagsList = new List<Tags>();
                    pic.Likes = 0;
                    pic.Visibility = !Visibility;
                    pic.UploadDate = DateTime.Now.Date;
                    pic.UserFK = user.Id;
                    user.PicturesList.Add(pic);
                    if (IsProfilePicture)
                    {
                        user.ProfilePhoto = pic.Path;
                    }
                    db.Users.AddOrUpdate(user);
                    db.Pictures.Add(pic);
                    db.SaveChanges();

                    ViewBag.Message = "File uploaded succesfully";
                }
                catch (Exception e)
                {
                    ViewBag.Message = "ERROR!" + e.Message.ToString();
                }
            }
            else
            {
                ViewBag.Message = "You didn't specified a file";
            }

            return RedirectToAction("MainPage", "Home", new { Id = Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePicture(int? Id)
        {
            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");

            //Session["DeleteSuccess"] = "No";
            Pictures pictureToDelete = db.Pictures.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();
            string userId = "";

            List<Users> users = db.Users.ToList();
            foreach (var user in users)
            {
                if (user.PicturesList.Contains(pictureToDelete))
                {
                    user.PicturesList.Remove(pictureToDelete);
                    userId = user.Id;
                    db.Users.AddOrUpdate(user);
                }
                if (user.LikedPictures.Contains(pictureToDelete))
                {
                    user.LikedPictures.Remove(pictureToDelete);
                    db.Users.AddOrUpdate(user);
                }
            }

            var photoPath = pictureToDelete.Path;
            string fullPath = Request.MapPath(photoPath);

            db.Pictures.Remove(pictureToDelete);
            db.SaveChanges();

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                //Session["DeleteSuccess"] = "Yes";
            }
            return RedirectToAction("MainPage", "Home", new { Id = userId });
        }

        public ActionResult ViewPicture(int PictureId)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index");
            Pictures pic = db.Pictures.Where(m => m.Id == PictureId).Select(m => m).SingleOrDefault();
            Users user = db.Users.Where(m => m == pic.User).Select(m => m).SingleOrDefault();

            var model = new UserPictureViewModels { User = user, Picture = pic };

            return View(model);
        }

        public ActionResult EditPicture(int PictureId)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index");
            Pictures pic = db.Pictures.Where(m => m.Id == PictureId).Select(m => m).SingleOrDefault();
            Users user = db.Users.Where(m => m == pic.User).Select(m => m).SingleOrDefault();

            var model = new UserPictureViewModels { User = user, Picture = pic };

            return View(model);
        }
        
        public ActionResult DownloadPicture(string PicturePath)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index");
            string path = AppDomain.CurrentDomain.BaseDirectory;
            byte[] fileBytes = System.IO.File.ReadAllBytes(path + PicturePath.Substring(PicturePath.LastIndexOf("UserPictures\\") - 1));
            string fileName = PicturePath.Substring(PicturePath.LastIndexOf("UserPictures\\") - 1);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult ViewOtherUserProfile(string UserId,string ViewedUserId)
        {
            if (!User.Identity.IsAuthenticated || UserId == null)
                return RedirectToAction("Index");

            Users user = db.Users.Where(m => m.Id == UserId).Select(m => m).SingleOrDefault();
            Users viewedUser = db.Users.Where(m => m.Id == ViewedUserId).Select(m => m).SingleOrDefault();

            var model = new CommunityProfilesViewModels {User = user,ViewedUser = viewedUser};
            return View(model);
        }

        public ActionResult EditProfile(string Id)
        {
            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");
            Users user = db.Users.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();

            return View(user);
        }
        [HttpPost]
        public ActionResult EditProfile(HttpPostedFileBase file, string Id)
        {
            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string path = Path.Combine(Server.MapPath("~/UserPictures"), Path.GetFileName("user" + Id + "_" + file.FileName));
                    file.SaveAs(path);

                    Users user = db.Users.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();
                    user.ProfilePhoto = path.Substring(path.IndexOf("UserPictures") - 1);

                    db.Users.AddOrUpdate(user);
                    db.SaveChanges();

                    ViewBag.Message = "File uploaded succesfully";
                }
                catch (Exception e)
                {
                    ViewBag.Message = "ERROR!" + e.Message.ToString();
                }
            }
            else
            {
                ViewBag.Message = "You didn't specified a file";
            }

            return RedirectToAction("MainPage", "Home", new { Id = Id });
        }

        public ActionResult Community(string Id)
        {
            if (!User.Identity.IsAuthenticated || Id == null)
                return RedirectToAction("Index");

            Users user = db.Users.Where(m => m.Id == Id).Select(m => m).SingleOrDefault();

            List<Pictures> pics = new List<Pictures>();
            foreach (var picture in db.Pictures)
                if (picture.Visibility)
                    pics.Add(picture);

            var model = new UserAllPicturesViewModels { User = user, Pictures = pics };

            return View(model);
        }

        public ActionResult LikePicture(string UserId, int PictureId)
        {
            Pictures PicBeingLiked = db.Pictures.Where(m => m.Id == PictureId).Select(m => m).SingleOrDefault();
            Users UserLikingPicture = db.Users.Where(m => m.Id == UserId).Select(m => m).SingleOrDefault();

            if (!UserLikingPicture.LikedPictures.Contains(PicBeingLiked))
            {
                UserLikingPicture.LikedPictures.Add(PicBeingLiked);
                PicBeingLiked.UsersThatLiked.Add(UserLikingPicture);
                PicBeingLiked.Likes++;

                db.Users.AddOrUpdate(UserLikingPicture);
                db.Pictures.AddOrUpdate(PicBeingLiked);
                db.SaveChanges();
            }

            return RedirectToAction("Community", "Home", new { Id = UserLikingPicture.Id });
        }
    }
}