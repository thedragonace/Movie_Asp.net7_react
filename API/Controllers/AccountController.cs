using System.Security.Claims;
using API.Services;
using Controllers.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;

namespace API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("Api/[Controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<UserApp> _manager;
    private readonly SignInManager<UserApp> _signInManager;
    private readonly TokenFactory _tokenFactory;

    public AccountController(UserManager<UserApp> manager, SignInManager<UserApp> signInManager, TokenFactory tokenFactory)
    {
        _manager = manager;
        _signInManager = signInManager;
        _tokenFactory = tokenFactory;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> UserLogin([FromBody] LoginDTO loginData)
    {
        var user = await _manager.Users.FirstOrDefaultAsync(x => x.Email == loginData.Email);

        if (user == null) return NotFound();

        var checker = await _signInManager.CheckPasswordSignInAsync(user, loginData.Password, false);

        if (checker.Succeeded)
        {
            return Ok(CreateUserDTO(user));
        }

        return Unauthorized();
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> UserRegister([FromBody] RegisterDTO user)
    {
        if (await _manager.Users.AnyAsync(data => data.Email == user.Email))
        {
            ModelState.AddModelError("Email", "Email Already Taken");
            return ValidationProblem();
        }
        if (await _manager.Users.AnyAsync(data => data.UserName == user.Username))
        {
            ModelState.AddModelError("Username", "Username Already Taken");
            return ValidationProblem();
        }

        UserApp newUser = new UserApp
        {
            DisplayName = user.Displayname,
            Email = user.Email,
            UserName = user.Username,
        };

        var result = await _manager.CreateAsync(newUser, user.Password);

        if (result.Succeeded)
        {
            return Ok(CreateUserDTO(newUser));
        }

        return StatusCode(StatusCodes.Status500InternalServerError);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<UserDTO>> GetUser()
    {
        var user = await _manager.Users.FirstOrDefaultAsync(x => x.Id == User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (user == null) return NotFound();

        return Ok(CreateUserDTO(user));
    }

    private UserDTO CreateUserDTO(UserApp user)
    {
        return new UserDTO
        {
            Displayname = user.DisplayName,
            Username = user.UserName,
            Token = _tokenFactory.CreateToken(user),
        };
    }
}