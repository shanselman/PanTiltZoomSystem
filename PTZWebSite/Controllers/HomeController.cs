using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PTZ;

namespace PTZWebSite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public void Move(int x, int y)
        {
            var p = PTZDevice.GetDevice(ConfigurationManager.AppSettings["DeviceName"], PTZType.Relative);
            p.Move(x,y);
        }

        [HttpPost]
        public void Zoom(int value)
        {
            var p = PTZDevice.GetDevice(ConfigurationManager.AppSettings["DeviceName"], PTZType.Relative);
            p.Zoom(value);
        }
    }
}
