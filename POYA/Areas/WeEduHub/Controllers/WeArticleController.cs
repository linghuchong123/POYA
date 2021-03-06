using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Ganss.XSS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using POYA.Areas.FunAdmin.Controllers;
using POYA.Areas.FunFiles.Controllers;
using POYA.Areas.WeEduHub.Models;
using POYA.Data;
using POYA.Unities.Helpers;
using X.PagedList;

namespace POYA.Areas.WeEduHub.Controllers
{
    [Authorize]
    [Area("WeEduHub")]
    public class WeArticleController : Controller
    {  
        #region     DI
        private readonly IWebHostEnvironment _webHostEnv;
        private readonly IStringLocalizer<Program> _localizer;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly X_DOVEHelper _x_DOVEHelper;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly HtmlSanitizer _htmlSanitizer;
        private readonly WeEduHubHelper _weEduHubHelper;
        private readonly FunFilesHelper _funFilesHelper;
        private readonly WeEduHubArticleClassHelper _weEduHubArticleClassHelper;
        private readonly FunAdminHelper _funAdminHelper;
        private readonly IConfiguration _configuration;
        public WeArticleController(
            IConfiguration configuration,
            HtmlSanitizer htmlSanitizer,
            SignInManager<IdentityUser> signInManager,
            X_DOVEHelper x_DOVEHelper,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnv,
            IStringLocalizer<Program> localizer)
        {
            _htmlSanitizer = htmlSanitizer;
            _webHostEnv = webHostEnv;
            _localizer = localizer;
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _x_DOVEHelper = x_DOVEHelper;
            _signInManager = signInManager;
            _configuration=configuration;
            _weEduHubArticleClassHelper=new WeEduHubArticleClassHelper(_webHostEnv );
            _weEduHubHelper=new WeEduHubHelper();
            _funFilesHelper=new FunFilesHelper();
            _funAdminHelper=new FunAdminHelper(_localizer,_context);
            WeArticleControllerInitial();
        }
        #endregion

        // GET: WeEduHub/WeArticle
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            Guid? SetId,
            int? APage
        )
        {
            var _UserId = _userManager.GetUserAsync(User).GetAwaiter().GetResult()?.Id;

            var _IllegalIds = await _context.FContentCheck.Where(p => p.ReceptionistId != string.Empty && !p.IsLegal).Select(p => p.ContentId).ToListAsync();

            var _WeArticles = _context.WeArticle
                .Where(
                    p =>
                        (SetId == null ? true : p.SetId == SetId) &&
                        !_IllegalIds.Contains(p.Id)
                );
            
            _WeArticles=(SetId == null ||SetId==Guid.Empty)? SortWeArticles( _WeArticles):(_WeArticles.OrderBy(p => p.DOPublishing));


            ViewData[nameof(SetId)] = SetId;

            ViewData["SetName"]=(SetId==null ||SetId==Guid.Empty)?string.Empty:await _context.WeArticleSet.Where(p=>p.Id==SetId).Select(p=>p.Name).FirstOrDefaultAsync();

            ViewData["SetBelongToCurrentUser"]=string.IsNullOrEmpty(_UserId)?false:await _context.WeArticleSet.AnyAsync(p=>p.Id==SetId && p.UserId==_UserId);

            ViewData["UserId"] = _UserId;

            ViewData["WeArticles"] = _WeArticles.ToPagedList(APage ?? 1, 10);

            return View();
        }

        // GET: WeEduHub/WeArticle/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var weArticle = await _context.WeArticle.Where(p=> p.Id==id).FirstOrDefaultAsync();

            if (weArticle == null)
            {
                return NotFound();
            }
            var _Author = _userManager.FindByIdAsync(weArticle.AuthorUserId).GetAwaiter().GetResult();

            ViewData["CurrentUserId"]=_userManager.GetUserAsync(User)?.GetAwaiter().GetResult()?.Id??string.Empty;

            ViewData["AuthorUserId"]=_Author.Id;
            ViewData["AuthorUserEmail"]=_Author.Email;

            _weEduHubArticleClassHelper.InitialWeArticleClassName(ref weArticle,weArticle.ClassId);

            ViewData["WeArticleFileContentType"]=_funFilesHelper.GetContentType(
                await _context.WeArticleFile.Where(p=>p.Id==weArticle.WeArticleContentFileId).Select(p=>p.Name).FirstOrDefaultAsync()
            );

            weArticle.SetName=await _context.WeArticleSet.Where(p=>p.Id==weArticle.SetId).Select(p=>p.Name).FirstOrDefaultAsync();

            return View(weArticle);
        }

        // GET: WeEduHub/WeArticle/Create
        public IActionResult Create(Guid? SetId)
        {
            if (SetId == null) return NotFound();

            var _WeArticle = new WeArticle 
                { 
                    SetId = SetId ?? Guid.Empty, 
                    ClassId=_weEduHubArticleClassHelper.GetAllSecondClasses().Select(p=>p.Id).FirstOrDefault(),
                    IsCommentBeAllowed=false,
                    IsNegativeSignBeAllowed=false,
                    IsPositiveSignBeAllowed=false
                };

            return View(_WeArticle);
        }

        // POST: WeEduHub/WeArticle/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1024*1024*1024)]
        public async Task<IActionResult> Create([Bind("SetId,Title,WeArticleFormFile,ClassId,CustomClass,Complex,Comment,IsCommentBeAllowed,IsNegativeSignBeAllowed,IsPositiveSignBeAllowed")]WeArticle weArticle)
        {
            if (ModelState.IsValid)
            {
                var _UserId = _userManager.GetUserAsync(User).GetAwaiter().GetResult().Id;

                if(
                    !await _context.WeArticleSet.AnyAsync(p=>p.UserId==_UserId && p.Id==weArticle.SetId) ||
                    weArticle.WeArticleFormFile==null 
                )
                {
                    return NotFound();
                }
                
                if(
                    !weArticle.WeArticleFormFile.ContentType.StartsWith("video/") &&
                    !weArticle.WeArticleFormFile.FileName.EndsWith(".pdf")
                )
                {
                    return BadRequest();
                }

                if(!IsFileLengthValid(weArticle.WeArticleFormFile))
                {
                    return BadRequest();
                }


                var _FormFileBytes=_funFilesHelper.GetFormFileBytes(weArticle.WeArticleFormFile);
                var _FormFileSHA256HexString=_funFilesHelper.SHA256BytesToHexString( 
                        SHA256.Create().ComputeHash(_FormFileBytes)
                    );
                    
                var WeEduHubFilesDirectoryInfo= new DirectoryInfo(_weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv));
                
                if(WeEduHubFilesDirectoryInfo.GetFiles().ToList().Any(p=>p.Name==_FormFileSHA256HexString))
                {
                    Console.WriteLine(_FormFileSHA256HexString);
                    return NotFound();
                }
                

                weArticle.Id = Guid.NewGuid();
                weArticle.AuthorUserId=_UserId;

                var _WeArticleFile=new WeArticleFile{
                        DOUploading=DateTimeOffset.Now,
                        Id=Guid.NewGuid(),
                        Name=System.IO.Path.GetFileName(weArticle.WeArticleFormFile.FileName),
                        UserId=_UserId,
                        SHA256HexString=_FormFileSHA256HexString
                    };

                weArticle.WeArticleContentFileId=_WeArticleFile.Id;

                weArticle.DOPublishing=DateTimeOffset.Now;

                await _context.WeArticle.AddAsync(weArticle);
                await _context.WeArticleFile.AddAsync( _WeArticleFile );

                await System.IO.File.WriteAllBytesAsync(
                    _weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv)+"/"+_FormFileSHA256HexString,
                    _FormFileBytes
                );

                await _context.SaveChangesAsync();
                return Content(Url.Action(nameof(Index),new{weArticle.SetId})); //  RedirectToAction(nameof(Index),new{weArticle.SetId});
            }
            return View(weArticle);
        }

        // GET: WeEduHub/WeArticle/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var _UserId = _userManager.GetUserAsync(User).GetAwaiter().GetResult().Id;

            var weArticle = await _context.WeArticle.Where(p=>p.AuthorUserId==_UserId && p.Id==id).FirstOrDefaultAsync();

            if (weArticle == null)
            {
                return NotFound();
            }

            ViewData["WeArticleFileContentType"]=_funFilesHelper.GetContentType(
                await _context.WeArticleFile.Where(p=>p.Id==weArticle.WeArticleContentFileId).Select(p=>p.Name).FirstOrDefaultAsync()
            );

            ViewData["FirstClassId"]=_weEduHubArticleClassHelper.GetFirstClassIdBySecondClassId(weArticle.ClassId);

            ViewData["IsEdit"]=true;


            return View("Create",weArticle);
        }

        // POST: WeEduHub/WeArticle/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1024*1024*1024)]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SetId,Title,WeArticleFormFile,ClassId,CustomClass,Complex,Comment,IsCommentBeAllowed,IsNegativeSignBeAllowed,IsPositiveSignBeAllowed")] WeArticle weArticle)
        {
            if (id != weArticle.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var _UserId = _userManager.GetUserAsync(User).GetAwaiter().GetResult().Id;
                    
                    var _WeArticle=await _context.WeArticle.Where(p=>p.Id==weArticle.Id && p.AuthorUserId==_UserId).FirstOrDefaultAsync();

                    if(weArticle.WeArticleFormFile!=null) 
                    {
                        
                        var _FormFileBytes=_funFilesHelper.GetFormFileBytes(weArticle.WeArticleFormFile);
                        var _FormFileSHA256HexString=_funFilesHelper.SHA256BytesToHexString( SHA256.Create().ComputeHash(_FormFileBytes));
                        var _WeEduHubFilesDirectoryInfo= new DirectoryInfo(_weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv));
                        
                        if(
                            _WeEduHubFilesDirectoryInfo.GetFiles().ToList().Any(p=>p.Name==_FormFileSHA256HexString) && 
                            !await _context.WeArticleFile.AnyAsync(p=>p.SHA256HexString==_FormFileSHA256HexString && p.UserId==_UserId)
                        )
                        {
                            return BadRequest();
                        }

                        if(!IsFileLengthValid(weArticle.WeArticleFormFile))
                        {
                            return BadRequest();
                        }

                        var _WeArticleFile=new WeArticleFile{
                            DOUploading=DateTimeOffset.Now,
                            Id=Guid.NewGuid(),
                            Name=System.IO.Path.GetFileName(weArticle.WeArticleFormFile.FileName),
                            UserId=_UserId,
                            SHA256HexString=_FormFileSHA256HexString
                        };

                        await System.IO.File.WriteAllBytesAsync(
                            _weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv)+"/"+_FormFileSHA256HexString,
                            _FormFileBytes
                        );

                        await _context.WeArticleFile.AddAsync(_WeArticleFile);
                        
                        _WeArticle.WeArticleContentFileId=_WeArticleFile.Id;
                    }
                    

                    _WeArticle.Title=weArticle.Title;
                    _WeArticle.Complex=weArticle.Complex;
                    _WeArticle.ClassId=weArticle.ClassId;
                    _WeArticle.CustomClass=weArticle.CustomClass;
                    _WeArticle.Comment=weArticle.Comment;
                    _WeArticle.IsCommentBeAllowed= weArticle.IsCommentBeAllowed;
                    _WeArticle.IsPositiveSignBeAllowed=weArticle.IsPositiveSignBeAllowed;
                    _WeArticle.IsNegativeSignBeAllowed=  weArticle.IsNegativeSignBeAllowed;
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WeArticleExists(weArticle.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                
                return Content(Url.Action(nameof(Index),new{weArticle.SetId})); //  RedirectToAction(nameof(Index), new{weArticle.SetId});
            }
            return View(weArticle);
        }

        // GET: WeEduHub/WeArticle/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var _UserId = _userManager.GetUserAsync(User).GetAwaiter().GetResult().Id;

            var weArticle = await _context.WeArticle.Where(p=>p.AuthorUserId==_UserId && p.Id==id).FirstOrDefaultAsync();
            if (weArticle == null)
            {
                return NotFound();
            }

            ViewData["WeArticleFileContentType"]=_funFilesHelper.GetContentType(
                await _context.WeArticleFile.Where(p=>p.Id==weArticle.WeArticleContentFileId).Select(p=>p.Name).FirstOrDefaultAsync()
            );

            return View(weArticle);
        }

        // POST: WeEduHub/WeArticle/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var weArticle = await _context.WeArticle.FindAsync(id);
            _context.WeArticle.Remove(weArticle);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WeArticleExists(Guid id)
        {
            return _context.WeArticle.Any(e => e.Id == id);
        }

        #region  DEPOLLUTION

        [AllowAnonymous]
        public async Task<IActionResult> WeArticleSignCountAsync(Guid WeArticleId, bool IsPositive=true)
        {
            return Content(
                Convert.ToString(await _context.WeArticleSign.Where(p=>p.WeArticleId==WeArticleId && p.IsPositive==IsPositive).CountAsync())
            );
        }

        public async Task<IActionResult> WeArticleSignCreateAsync(Guid WeArticleId, bool IsPositive=true)
        {
            if(!_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("Account/Login",new{area="Identity"});
            }
            var _WeArticle=await _context.WeArticle.Where(p=>p.Id==WeArticleId).FirstOrDefaultAsync();

            if(_WeArticle==null)
            {
                return BadRequest();
            }

            var _UserId = _userManager.GetUserAsync(User).GetAwaiter().GetResult().Id;

            var _WeArticleSign=await _context.WeArticleSign.Where(p=>p.UserId==_UserId && p.WeArticleId==WeArticleId).FirstOrDefaultAsync();

            if(_WeArticleSign==null)
            {
                await _context.WeArticleSign.AddAsync(
                    new WeArticleSign{
                        DOSigning=DateTimeOffset.Now,
                        Id=Guid.NewGuid(),
                        IsPositive=IsPositive,
                        UserId=_UserId,
                        WeArticleId=WeArticleId,
                    }
                );
            }
            else
            {
                _context.WeArticleSign.Remove(_WeArticleSign);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        public IActionResult FunCommentsIndex(Guid WeArticleId)
        {
            return ViewComponent("FunComments",new{ViewName="Index",WeArticleId});
        }

        private bool IsFileLengthValid(IFormFile _WeArticleFormFile)
        {
            return _WeArticleFormFile.ContentType.StartsWith("video/")?
                _WeArticleFormFile.Length < Convert.ToInt32(_configuration["WeEduHub.WeArticle:AllowedVideoFileLength"]):
                _WeArticleFormFile.Length < Convert.ToInt32(_configuration["WeEduHub.WeArticle:AllowedDocumentFileLength"]);
        }

        /// <summary>
        /// Prevent file duplication
        /// </summary>
        /// <returns></returns>
        public IActionResult IsFileDuplicate(string FileSHA256HexString)
        {   
            FileSHA256HexString=FileSHA256HexString.ToLower();
            var _DirectoryInfo=new DirectoryInfo(_weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv));

            var _FileNames=_DirectoryInfo.GetFiles().Select(p=>p.Name.ToLower()).ToList();
            return Content(_FileNames.Any(p=>p==FileSHA256HexString).ToString());
        }

        
        private IQueryable<WeArticle> SortWeArticles(IQueryable<WeArticle> _WeArticles)
        {
            var WEARTICLE_SORT_BY = Request.Cookies[_weEduHubHelper.WEARTICLE_SORT_BY_String] ?? _weEduHubHelper.SortByDate;
            
            var IS_WEARTICLE_ORDER_BY_ASC = Request.Cookies[_weEduHubHelper.IS_WEARTICLE_ORDER_BY_ASC_String]?.ToString() == "true";
            
            if (WEARTICLE_SORT_BY == _weEduHubHelper.SortByTitle)
            {
                if (IS_WEARTICLE_ORDER_BY_ASC)
                {
                    _WeArticles=_WeArticles.OrderBy(p => p.Title);
                }
                else
                {
                    _WeArticles=_WeArticles.OrderByDescending(p => p.Title);
                }
            }
            else if (WEARTICLE_SORT_BY == _weEduHubHelper.SortByModifying)
            {
                if (IS_WEARTICLE_ORDER_BY_ASC)
                {
                    _WeArticles=_WeArticles.OrderBy(p => p.DOModifying);
                }
                else
                {
                    _WeArticles=_WeArticles.OrderByDescending(p => p.DOModifying);
                }
            }
            else
            {
                if (IS_WEARTICLE_ORDER_BY_ASC)
                {
                   _WeArticles= _WeArticles.OrderBy(p => p.DOPublishing);
                }
                else
                {
                    _WeArticles=_WeArticles.OrderByDescending(p => p.DOPublishing);
                }
            }

            ViewData[_weEduHubHelper.WEARTICLE_SORT_BY_String] = WEARTICLE_SORT_BY;
            ViewData[_weEduHubHelper.IS_WEARTICLE_ORDER_BY_ASC_String]=IS_WEARTICLE_ORDER_BY_ASC;
            return _WeArticles;
        }


        [AllowAnonymous]
        [ActionName("GetWeArticleFile")]
        public async Task<IActionResult> GetWeArticleFileAsync(Guid? Id)
        {
            if(Id==null)
            {
                return NotFound();
            }

            var _WeArticleFile=await _context.WeArticleFile.Where(p=>p.Id==Id).FirstOrDefaultAsync();

            if(_WeArticleFile==null)
            {
                return NotFound();
            }

            var _WeArticleFileBytes=await System.IO.File.ReadAllBytesAsync(_weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv)+"/"+_WeArticleFile.SHA256HexString);

            return File(_WeArticleFileBytes,_funFilesHelper.GetContentType(_WeArticleFile.Name),true);
        }

        public IActionResult GetSecondClassesByFirstClassCode(string FirstClassCode)
        {
            var _SecondClasses=_weEduHubArticleClassHelper.GetSecondClassesByFirstClassCode(FirstClassCode);
            return View("FirstClassesSelect",_SecondClasses);
        }

        public IActionResult GetAllFirstClasses()
        {
            var _WeArticleFirstClasses=_weEduHubArticleClassHelper.GetAllFirstClasses();

            return View("FirstClassesSelect",_WeArticleFirstClasses);
        }

        private void WeArticleControllerInitial()
        {
            if(!System.IO.Directory.Exists( _weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv))) 
                System.IO.Directory.CreateDirectory(_weEduHubHelper.WeEduHubFilesDirectoryPath(_webHostEnv) );
            
        }
        #endregion
    }
}
