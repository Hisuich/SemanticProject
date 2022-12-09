using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Builder;

namespace SemanticProject.Services
{
    public interface IQueryService
    {
        static public List<string> ms_langs = new List<string>(){ "en", "ru"};
        static public Dictionary<string, Uri> ms_prefixes = new Dictionary<string, Uri>()
        {
            {"dbo", new Uri("http://dbpedia.org/ontology/") },
            {"rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#") },
            {"rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#") }
        };
        abstract SparqlRemoteEndpoint GetEndpoint();
        abstract NamespaceMapper GetPrefixes();
        abstract void AddVariable(string name, bool isShown);
        abstract void AddTriplet(string subject, string predicate, string _object, bool isObjectNode);
        abstract void AddFilter(Func<INonAggregateExpressionBuilder, BooleanExpression> filter);
        abstract void Union(int unionId = 0);
        abstract int GetUnion();
        abstract void Optional();
        abstract string GetLang();
        abstract string GetQuery();
        abstract string GetResult();
        abstract void SetDistinct(bool distinct);
        abstract SparqlResultSet GetResultSet();
        abstract List<Tuple<string, string>> GetResultList();
        abstract void SetQueryLimit(int limit);
        abstract void SetQueryOffset(int offset);
        abstract void SetOrderBy(string orderBy, bool isAsc = true);

        abstract void Clear();
        abstract void SetLang(string lang);
    }
}
