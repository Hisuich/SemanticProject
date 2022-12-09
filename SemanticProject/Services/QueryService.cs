using Swashbuckle.AspNetCore.SwaggerGen;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Shacl.Validation;

namespace SemanticProject.Services
{
    public class QueryService : IQueryService
    {
        private class M_Variable
        {
           public string m_name;
           public bool m_isShown;
        }

        private class M_Triplet
        {
            public M_Variable m_subject;
            public string m_predicate;
            public M_Variable m_object;
            public bool m_isObjectNode;
        }

        const int OPTIONAL_ID = -1;
        const int STANDARD_ID = 0;

        private SparqlRemoteEndpoint m_endpoint;
        private string m_lang;

        private List<Tuple<int, M_Triplet>> m_triplets;
        private HashSet<M_Variable> m_variables;
        private List<Tuple<int, Func<INonAggregateExpressionBuilder, BooleanExpression>>> m_filters;
        private int m_limit;
        private int m_offset;
        private int m_unionId;
        private string m_orderBy;
        private bool m_isAsc;
        private bool m_isDistinct;

        public QueryService()
        {
            m_endpoint = new SparqlRemoteEndpoint(new Uri("http://dbpedia.org/sparql"), "http://dbpedia.org");
            m_triplets = new List<Tuple<int, M_Triplet>>();
            m_variables = new HashSet<M_Variable>();
            m_filters = new List<Tuple<int, Func<INonAggregateExpressionBuilder, BooleanExpression>>>();

            Clear();
        }

        public SparqlRemoteEndpoint GetEndpoint()
        {
            return m_endpoint;
        }

        public string GetLang()
        {
            return m_lang;
        }

        public NamespaceMapper GetPrefixes()
        {
            var prefixes = new NamespaceMapper();

            foreach (var p in IQueryService.ms_prefixes)
            {
                prefixes.AddNamespace(p.Key, p.Value);
            }

            return prefixes;
        }

        public void SetLang(string lang)
        {
            if (IQueryService.ms_langs.Contains(lang)) m_lang = lang;
        }

        private M_Variable GetVariable(string name)
        {
            M_Variable variable = null;
            foreach (var variab in m_variables)
            {
                if (variab.m_name == name)
                {
                    variable = variab;
                }
            }
            if (variable == null)
            {
                AddVariable(name, false);
                variable = new M_Variable() { m_name = name, m_isShown = false };
            }
            return variable;
        }
        
        public void AddVariable(string name, bool isShown)
        {
            m_variables.Add(new M_Variable() { m_name = name, m_isShown = isShown });
        }
        public void SetDistinct(bool distinct)
        {
            m_isDistinct = distinct;
        }

        public void AddTriplet(string subject, string predicate, string _object, bool isObjectNode)
        {
            m_triplets.Add(new Tuple<int, M_Triplet>(m_unionId, new M_Triplet()
            {
                m_subject = GetVariable(subject),
                m_predicate = predicate,
                m_object = GetVariable(_object),
                m_isObjectNode = isObjectNode
            }));
        }

        public void AddFilter(Func<INonAggregateExpressionBuilder, BooleanExpression> filter)
        {
            m_filters.Add(new Tuple<int, Func<INonAggregateExpressionBuilder, BooleanExpression>>(m_unionId, filter));
        }

        public void Union(int unionId = STANDARD_ID)
        {
            m_unionId = unionId;
        }

        public void Optional()
        {
            m_unionId = OPTIONAL_ID;
        }

        public string GetQuery()
        {
            bool isUnion = false;
            bool isOptional = false;
            HashSet<int> Ids = new HashSet<int>();
            foreach (var triplet in m_triplets)
            {
                if (triplet.Item1 != STANDARD_ID && triplet.Item1 != OPTIONAL_ID)
                {
                    isUnion = true;
                    Ids.Add(triplet.Item1);
                }
                if (triplet.Item1 == OPTIONAL_ID)
                {
                    isOptional = true;
                }
            }

            List<string> shownVars = new List<string>();
            foreach (var variable in m_variables)
            {
                if (variable.m_isShown) shownVars.Add(variable.m_name);
            }

            IQueryBuilder queryBuilder;
            ISelectBuilder select = QueryBuilder.Select(shownVars.ToArray());
            if (m_isDistinct)
            {
                select = select.Distinct();
            }
            queryBuilder = select
                .Where(
                    (triplePatternBuilder) =>
                    {
                        foreach (var triplet in m_triplets)
                        {
                            if (triplet.Item1 == STANDARD_ID)
                            {
                                if (triplet.Item2.m_isObjectNode)
                                {
                                    triplePatternBuilder
                                    .Subject(triplet.Item2.m_subject.m_name)
                                    .PredicateUri(triplet.Item2.m_predicate)
                                    .Object<IUriNode>(triplet.Item2.m_object.m_name);
                                }
                                else
                                {
                                    triplePatternBuilder
                                    .Subject(triplet.Item2.m_subject.m_name)
                                    .PredicateUri(triplet.Item2.m_predicate)
                                    .Object(triplet.Item2.m_object.m_name);
                                }
                            }
                        }
                    });
            foreach (var filter in m_filters)
            {
                if (filter.Item1 == STANDARD_ID)
                {
                    queryBuilder.Filter(filter.Item2);
                }
            }

            

            foreach (int i in Ids)
            {
                queryBuilder.Union(
                    (unionBuilder) =>
                    {
                        unionBuilder.Where(
                            (triplePatternBuilder) =>
                            {
                                foreach (var triplet in m_triplets)
                                {
                                    if (triplet.Item1 == i)
                                    {
                                        if (triplet.Item2.m_isObjectNode)
                                        {
                                            triplePatternBuilder
                                            .Subject(triplet.Item2.m_subject.m_name)
                                            .PredicateUri(triplet.Item2.m_predicate)
                                            .Object<IUriNode>(triplet.Item2.m_object.m_name);
                                        }
                                        else
                                        {
                                            triplePatternBuilder
                                            .Subject(triplet.Item2.m_subject.m_name)
                                            .PredicateUri(triplet.Item2.m_predicate)
                                            .Object(triplet.Item2.m_object.m_name);
                                        }
                                    }
                                }
                            }
                            );
                        foreach (var filter in m_filters)
                        {
                            if (filter.Item1 == i)
                            {
                                unionBuilder.Filter(filter.Item2);
                            }
                        }
                    }
                    );
            }
            queryBuilder.Optional(
                (optionalBuilder) =>
                {
                    optionalBuilder.Where(
                        (triplePatternBuilder) =>
                        {
                            foreach (var triplet in m_triplets)
                            {
                                if (triplet.Item1 == OPTIONAL_ID)
                                {
                                    if (triplet.Item2.m_isObjectNode)
                                    {
                                        triplePatternBuilder
                                        .Subject(triplet.Item2.m_subject.m_name)
                                        .PredicateUri(triplet.Item2.m_predicate)
                                        .Object<IUriNode>(triplet.Item2.m_object.m_name);
                                    }
                                    else
                                    {
                                        triplePatternBuilder
                                        .Subject(triplet.Item2.m_subject.m_name)
                                        .PredicateUri(triplet.Item2.m_predicate)
                                        .Object(triplet.Item2.m_object.m_name);
                                    }
                                }
                            }
                        }
                        );
                    foreach (var filter in m_filters)
                    {
                        if (filter.Item1 == OPTIONAL_ID)
                        {
                            optionalBuilder.Filter(filter.Item2);
                        }
                    }
                }
                );

            if (m_orderBy != null)
            {
                if (m_isAsc) queryBuilder.OrderBy(m_orderBy);
                else queryBuilder.OrderByDescending(m_orderBy);
            }

            if (m_limit != -1)
            {
                queryBuilder.Limit(m_limit);
            }

            if (m_offset != -1)
            {
                queryBuilder.Offset(m_offset);
            }

            queryBuilder.Prefixes = GetPrefixes();

            return queryBuilder.BuildQuery().ToString();
        }

        public string GetResult()
        {
            string result = "";
            SparqlResultSet s = GetResultSet();
            foreach (var t in s)
            {
                result += t.ToString() + "\n";
            }
            

            return result;
        }

        public SparqlResultSet GetResultSet()
        {
            string queryString = GetQuery();
            var results = m_endpoint.QueryWithResultSet(queryString.ToString());
            results.ToList();
            return results;
        }

        public List<Tuple<string,string>> GetResultList()
        {
            string queryString = GetQuery();
            var results = m_endpoint.QueryWithResultSet(queryString.ToString());
            List<Tuple<string, string>> list = new List<Tuple<string, string>>();
  
            for (int i = 0; i < results.Count; i++)
            {
                foreach (var rs in results[i])
                {
                    list.Add(new Tuple<string, string>(rs.Key, rs.Value != null ? rs.Value.ToString() : ""));
                }
            }

            return list;
        }


        public void SetQueryLimit(int limit)
        {
            m_limit = limit;
        }
        public void SetQueryOffset(int offset)
        {
            m_offset = offset;
        }

        public void Clear()
        {
            m_limit = -1;
            m_offset = -1;
            m_isDistinct = false;
            m_triplets.Clear();
            m_variables.Clear();
            m_filters.Clear();
            SetLang("en");
            m_unionId = 0;
        }

        public void SetOrderBy(string orderBy, bool isAsc = true)
        {
            m_orderBy = orderBy;
            m_isAsc = isAsc;
        }

        public int GetUnion()
        {
            return m_unionId;
        }
    }
}
