using System.Xml.Linq;
using HaWeb.Models;

public class EditCollection : HaWeb.XMLParser.IXMLCollection {
    public string Key { get; } = "edits";
    public string[] xPath { get; } = new string[] { 
        "/opus/data/document/letterText//edit", 
        "/opus/document/letterText//edit", 
        "/opus/data/traditions/letterTradition//edit", 
        "/opus/traditions/letterTradition//edit"
    };
    public Func<XElement, string?> GenerateKey { get; } = GetKey;
    public Func<XElement, IDictionary<string, string>?>? GenerateDataFields { get; } = null;
    public Func<IEnumerable<CollectedItem>, IDictionary<string, ILookup<string, CollectedItem>>?>? GroupingsGeneration { get; } = null;
    public Func<IEnumerable<CollectedItem>, IDictionary<string, IEnumerable<CollectedItem>>?>? SortingsGeneration { get; } = null;
    public HaWeb.XMLParser.IXMLCollection[]? SubCollections { get; } = null;
    public bool Searchable { get; } = true;

    public static Func<XElement, string?> GetKey { get; } = (elem) => {
        var index = elem.Attribute("ref");
        if (index != null && !String.IsNullOrWhiteSpace(index.Value))
            return index.Value;
        else return null;
    };
}