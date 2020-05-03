using covidSim.Services;
using Microsoft.AspNetCore.Mvc;

namespace covidSim.Controllers
{
    
    public class GameController : Controller
    {
        [HttpGet]
        [Route("api/state")]
        public IActionResult State()
        {
            var game = Game.Instance;
            game = game.GetNextState();
            return Ok(game);
        }

        [HttpGet]
        [Route("api/restart")]
        public IActionResult Restart()
        {
            Game.Restart();
            return Ok(Game.Instance);
        }
    }
}