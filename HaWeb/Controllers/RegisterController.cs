namespace HaWeb.Controllers;
using Microsoft.AspNetCore.Mvc;
using HaWeb.Models;
using HaXMLReader.Interfaces;
using HaDocument.Models;
using HaWeb.FileHelpers;

[Route("/HKB/Register/[action]/{id?}")]
public class RegisterController : Controller {
    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }

    // DI
    private IHaDocumentWrappper _lib;
    private IReaderService _readerService;

    public RegisterController(IHaDocumentWrappper lib, IReaderService readerService) {
        _lib = lib;
        _readerService = readerService;
    }

    [HttpGet]
    public IActionResult Allgemein(string? id, string? search) {
        // Setup settings and variables
        var lib = _lib.GetLibrary();
        var url = "/HKB/Register/Allgemein/";
        var category = "neuzeit";
        var defaultLetter = "A";
        var title = "Allgemeines Register";
        ViewData["Title"] = "HKB – Allgemeines Register";
        ViewData["SEODescription"] = "Johann Georg Hamann: Kommentierte Briefausgabe. Personen-, Sach- und Ortsregister.";

        // Normalisation and validation
        if (String.IsNullOrWhiteSpace(id)) return Redirect(url + defaultLetter);
        id = normalizeID(id, defaultLetter);
        if (String.IsNullOrWhiteSpace(id)) return Redirect(url + defaultLetter);
        if (!lib.CommentsByCategoryLetter[category].Contains(id!)) return error404();

        // Data aquisition and validation
        var comments = lib.CommentsByCategoryLetter[category][id].OrderBy(x => x.Index);
        var availableCategories = lib.CommentsByCategoryLetter[category].Select(x => (x.Key.ToUpper(), url + x.Key.ToUpper())).OrderBy(x => x.Item1).ToList();
        if (comments == null) return error404();

        // Parsing
        var res = _createCommentModelForschungRegister(category, comments); 

        // Model instantiation
        var model = new RegisterViewModel(category, id, res, title, false, true) {
            AvailableCategories = availableCategories,
        };

        // Return
        return View("~/Views/HKB/Dynamic/Register.cshtml", model);
    }

    [HttpGet]
    public IActionResult Bibelstellen(string? id) {
        // Setup settings and variables
        var lib = _lib.GetLibrary();
        var url = "/HKB/Register/Bibelstellen/";
        var category = "bibel";
        var defaultLetter = "AT";
        var title = "Bibelstellenregister";
        ViewData["Title"] = "HKB – Bibelstellenregister";
        ViewData["SEODescription"] = "Johann Georg Hamann: Kommentierte Briefausgabe. Bibelstellenregister.";

        // Normalisation and Validation
        if (id == null) return Redirect(url + defaultLetter);
        id = normalizeID(id, defaultLetter);
        if (id != "AT" && id != "AP" && id != "NT") return error404();

        // Data aquisition and validation
        var comments = lib.CommentsByCategory[category].ToLookup(x => x.Index.Substring(0, 2).ToUpper())[id].OrderBy(x => x.Order);
        var availableCategories = new List<(string, string)>() { ("Altes Testament", url + "AT"), ("Apogryphen", url + "AP"), ("Neues Testament", url + "NT") };
        if (comments == null) return error404();

        // Parsing
        var res = _createCommentModelBibel(category, comments);

        // Model instantiation
        var model = new RegisterViewModel(category, id, res, title, false, false) {
            AvailableCategories = availableCategories,
        };

        // Return
        return View("~/Views/HKB/Dynamic/Register.cshtml", model);
    }

    [HttpGet]
    public IActionResult Forschung(string? id) {
        // Setup settings and variables
        var lib = _lib.GetLibrary();
        var url = "/HKB/Register/Forschung/";
        var category = "forschung";
        var defaultLetter = "A";
        var title = "Forschungsbibliographie";
        ViewData["Title"] = "HKB – Forschungsbibliographie";
        ViewData["SEODescription"] = "Johann Georg Hamann: Kommentierte Briefausgabe. Forschungsbibliographie.";

        // Normalisation and Validation
        if (String.IsNullOrWhiteSpace(id)) return Redirect(url + defaultLetter);
        id = normalizeID(id, defaultLetter);
        if (String.IsNullOrWhiteSpace(id)) return Redirect(url + defaultLetter);
        if (id != "EDITIONEN" && id != "NACHSCHLAGEWERKE" && !lib.CommentsByCategoryLetter[category].Contains(id)) return error404();
        if ((id == "EDITIONEN" || id == "NACHSCHLAGEWERKE") && !lib.CommentsByCategoryLetter.Keys.Contains(id.ToLower())) return error404();

        // Data aquisition and validation
        IOrderedEnumerable<Comment>? comments = null;
        if (id == "EDITIONEN" || id == "NACHSCHLAGEWERKE") {
            comments = lib.CommentsByCategory[id.ToLower()].OrderBy(x => x.Index);
        } else {
            comments = lib.CommentsByCategoryLetter[category][id].OrderBy(x => x.Index);
        }
        var availableCategories = lib.CommentsByCategoryLetter[category].Select(x => (x.Key.ToUpper(), url + x.Key.ToUpper())).OrderBy(x => x.Item1).ToList();
        var AvailableSideCategories = new List<(string, string)>() { ("Editionen", "Editionen"), ("Nachschlagewerke", "Nachschlagewerke") };
        if (comments == null) return error404();

        // Parsing
        var res = _createCommentModelForschungRegister(category, comments, false);

        // Model instantiation
        var model = new RegisterViewModel(category, id, res, title, true, true) {
            AvailableCategories = availableCategories,
            AvailableSideCategories = AvailableSideCategories
        };

        // Return
        return View("~/Views/HKB/Dynamic/Register.cshtml", model);
    }

    private string? normalizeID(string? id, string defaultid) {
        if (id == null) return null;
        return id.ToUpper();
    }

    private bool validationCheck(string? id, HashSet<string> permitted) {
        if (id == null) return false;
        if (!permitted.Contains(id)) {
            return false;
        }
        return true;
    }

    private IActionResult error404() {
        Response.StatusCode = 404;
        return Redirect("/Error404");
    }

    private List<CommentModel> _createCommentModelForschungRegister(string category, IOrderedEnumerable<Comment>? comments, bool generateBacklinks = true) {
        var lib = _lib.GetLibrary();
        var res = new List<CommentModel>();
        if (comments == null) return res;
        foreach (var comm in comments) {
            var parsedComment = HTMLHelpers.CommentHelpers.CreateHTML(lib, _readerService, comm, category, Settings.ParsingState.CommentType.Comment, generateBacklinks);
            List<string>? parsedSubComments = null;
            if (comm.Kommentare != null) {
                parsedSubComments = new List<string>();
                foreach (var subcomm in comm.Kommentare.OrderBy(x => x.Value.Order)) {
                    parsedSubComments.Add(HTMLHelpers.CommentHelpers.CreateHTML(lib, _readerService, subcomm.Value, category, Settings.ParsingState.CommentType.Subcomment, generateBacklinks));
                }
            }
            res.Add(new CommentModel(parsedComment, parsedSubComments, comm.Index));
        }
        return res;
    }

    private List<CommentModel> _createCommentModelBibel(string category, IOrderedEnumerable<Comment>? comments) {
        var lib = _lib.GetLibrary();
        var res = new List<CommentModel>();
        if (comments == null) return res;
        foreach (var comm in comments) {
            var parsedComment = HTMLHelpers.CommentHelpers.CreateHTML(lib, _readerService, comm, category, Settings.ParsingState.CommentType.Comment);
            List<string>? parsedSubComments = null;
            if (comm.Kommentare != null) {
                parsedSubComments = new List<string>();
                foreach (var subcomm in comm.Kommentare.OrderBy(x => x.Value.Lemma.Length).ThenBy(x => x.Value.Lemma)) {
                    parsedSubComments.Add(HTMLHelpers.CommentHelpers.CreateHTML(lib, _readerService, subcomm.Value, category, Settings.ParsingState.CommentType.Subcomment));
                }
            }
            res.Add(new CommentModel(parsedComment, parsedSubComments, comm.Index));
        }
        return res;
    }
}