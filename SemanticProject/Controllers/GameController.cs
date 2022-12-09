using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using SemanticProject.Services;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Builder.Expressions;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Shacl.Validation;
using VDS.RDF.Writing.Formatting;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Comparison;
using VDS.RDF.Query.Expressions.Primary;
using QueryBuilder = VDS.RDF.Query.Builder.QueryBuilder;

namespace SemanticProject.Controllers
{
    public enum ResultType
    { 
        E_STRING,
        E_LIST,
        E_SET
    }


    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        private QueryService m_queryService;

        private const int DEFAULT_NUMBER = 50;

        private string game = "game",
                    gameName = "gameName",
                    paramName = "paramName",
                    description = "description",
                    platforms = "platforms",
                    developer = "developer",
                    engine = "engine",
                    genre = "genre",
                    releaseDate = "releaseDate",
                    series = "series";

        private Dictionary<string, Type> m_paramsTypes = new Dictionary<string, Type>();

        private Dictionary<string, string> m_gamePredicates = new Dictionary<string, string>();

        private HashSet<string> m_paramsNames = new HashSet<string>();


        private struct LinkType
        {
        }


        public GameController()
        {
            m_queryService = new QueryService();

            m_gamePredicates.Add(gameName, "rdfs:label");
            m_gamePredicates.Add(paramName, "rdfs:label");
            m_gamePredicates.Add(description, "dbo:abstract");
            m_gamePredicates.Add(platforms, "dbo:computePlatform");
            m_gamePredicates.Add(developer, "dbo:developer");
            m_gamePredicates.Add(engine, "dbo:gameEngine");
            m_gamePredicates.Add(genre, "dbo:genre");
            m_gamePredicates.Add(releaseDate, "dbo:releaseDate");
            m_gamePredicates.Add(series, "dbo:series");

            m_paramsNames.Add(game);
            m_paramsNames.Add(gameName);
            m_paramsNames.Add(paramName);
            m_paramsNames.Add(description);
            m_paramsNames.Add(platforms);
            m_paramsNames.Add(developer);
            m_paramsNames.Add(engine);
            m_paramsNames.Add(genre);
            m_paramsNames.Add(releaseDate);
            m_paramsNames.Add(series);

            m_paramsTypes.Add(gameName, typeof(string));
            m_paramsTypes.Add(description, typeof(string));
            m_paramsTypes.Add(platforms, typeof(LinkType));
            m_paramsTypes.Add(developer, typeof(LinkType));
            m_paramsTypes.Add(engine, typeof(LinkType));
            m_paramsTypes.Add(genre, typeof(LinkType));
            m_paramsTypes.Add(releaseDate, typeof(uint));
            m_paramsTypes.Add(series, typeof(LinkType));
        }

        private void QueryGamesInPages(int number, int page)
        {
            m_queryService.SetQueryLimit(number);
            m_queryService.SetQueryOffset(page * number);
        }

        private void QueryGroupByParameter(string parameter, bool asc)
        {

            m_queryService.SetOrderBy(parameter, asc);
        }

        private IActionResult GetResult(ResultType resType)
        {
            switch (resType)
            {
                case ResultType.E_STRING:
                    return Ok(m_queryService.GetResult());
                    break;
                case ResultType.E_LIST:
                    return Ok(m_queryService.GetResultList());
                    break;
                case ResultType.E_SET:
                    return Ok(m_queryService.GetResultSet());
                    break;
                default:
                    return Ok(m_queryService.GetResult());
            }
        }

        [Route("GetAllGames")]
        [HttpGet]
        public IActionResult GetGames(ResultType resType = ResultType.E_STRING)
        {

            m_queryService.AddVariable(game, true);
            m_queryService.AddVariable(gameName, true);

            m_queryService.AddTriplet(game, "rdf:type", "dbo:VideoGame", true);
            m_queryService.AddTriplet(game, m_gamePredicates[gameName], gameName, false);

            m_queryService.AddFilter((builder) => builder.Lang(builder.Variable(gameName)) == m_queryService.GetLang());

            var result = GetResult(resType);

            m_queryService.Clear();

            return result;

        }

        [Route("GetAllValueOfGameParam")]
        [HttpGet]
        public IActionResult GetAllValueOfGameParam(string param, int number = DEFAULT_NUMBER, int page = 0, ResultType resType = ResultType.E_STRING)
        {
            if (m_paramsNames.Contains(param))
            {
                m_queryService.Clear();

                m_queryService.AddVariable(game, false);
                m_queryService.AddVariable(param, true);
                m_queryService.AddVariable(paramName, true);

                m_queryService.AddTriplet(game, "rdf:type", "dbo:VideoGame", true);
                m_queryService.AddTriplet(game, m_gamePredicates[param], param, false);
                m_queryService.AddTriplet(param, m_gamePredicates[paramName], paramName, false);

                    m_queryService.AddFilter((builder) => builder.Lang(builder.Variable(paramName)) == m_queryService.GetLang());

                QueryGamesInPages(number, page);

                m_queryService.SetDistinct(true);
            }

            var result = GetResult(resType);

            m_queryService.Clear();

            return result;
        }

        [Route("GetGameByParamName")]
        [HttpGet]
        public IActionResult GetGamesByParamName(string parameter, string value, int number = DEFAULT_NUMBER, int page = 0, ResultType resType = ResultType.E_STRING)
        {
            if (m_paramsNames.Contains(parameter))
            {
                m_queryService.Clear();

                m_queryService.AddVariable(game, true);
                m_queryService.AddVariable(parameter, true);

                m_queryService.AddTriplet(game, "rdf:type", "dbo:VideoGame", true);
                m_queryService.AddTriplet(game, m_gamePredicates[parameter], parameter, false);

                m_queryService.AddFilter((builder) => builder.Lang(builder.Variable(parameter)) == m_queryService.GetLang());
                m_queryService.AddFilter((builder) => builder.Str(builder.Variable(parameter)) == value);

                QueryGamesInPages(number, page);

                var result = GetResult(resType);

                m_queryService.Clear();

                return result;

            }
            return Ok();
        }

        [Route("GetGameParameters")]
        [HttpGet]
        public IActionResult QueryGameParameters()
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            foreach (var param in m_gamePredicates)
            {
                result.Add(new Tuple<string, string>(param.Key, param.Value));
            }

            return Ok(result);
        }

        [Route("GetGamesList")]
        [HttpPost]
        public IActionResult QueryGamesList(string orderBy = "-", bool isAsc = true, int number = DEFAULT_NUMBER, int page = 0, string[] parameters = null, ResultType resType = ResultType.E_STRING)
        { 
            m_queryService.Clear();
            m_queryService.AddVariable(game, true);
            m_queryService.AddVariable(gameName, true);

            m_queryService.AddTriplet(game, "rdf:type", "dbo:VideoGame", true);
            m_queryService.AddTriplet(game, m_gamePredicates[gameName], gameName, false);

            m_queryService.Optional();
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    string parameter = parameters[i];
                    if (m_paramsNames.Contains(parameter))
                    {
                        m_queryService.AddVariable(parameter, true);
                        m_queryService.AddTriplet(game, m_gamePredicates[parameter], parameter, false);

                        if (m_paramsTypes[parameter] == typeof(string))
                            m_queryService.AddFilter((builder) => builder.Lang(builder.Variable(parameter)) == m_queryService.GetLang());

                    }
                }
            }

            m_queryService.Union();

            m_queryService.SetQueryLimit(number);
            m_queryService.SetQueryOffset(page * number);

            if (m_paramsNames.Contains(orderBy))
            {
                m_queryService.SetOrderBy(orderBy, isAsc);
            }
            
            m_queryService.AddFilter((builder) => builder.Lang(builder.Variable(gameName)) == m_queryService.GetLang());

            var result = GetResult(resType);

            m_queryService.Clear();

            return result;
        }

        [Route("GetInfoAboutGameByNumber")]
        [HttpGet]
        public IActionResult QueryGameByNumber(int number, ResultType resType = ResultType.E_STRING)
        {
            if (number < 0)
                return Ok();

            else
            {
                m_queryService.Clear();

                m_queryService.AddVariable(game, true);
                m_queryService.AddVariable(gameName, true);
                m_queryService.AddVariable(description, true);
                m_queryService.AddVariable(platforms, true);
                m_queryService.AddVariable(developer, true);
                m_queryService.AddVariable(engine, true);
                m_queryService.AddVariable(genre, true);
                m_queryService.AddVariable(releaseDate, true);
                m_queryService.AddVariable(series, true);

                m_queryService.AddTriplet(game, "rdf:type", "dbo:VideoGame", true);
                m_queryService.AddTriplet(game, m_gamePredicates[gameName], gameName, false);

                m_queryService.Optional();

                m_queryService.AddTriplet(game, m_gamePredicates[description], description, false);
                m_queryService.AddTriplet(game, m_gamePredicates[platforms], platforms, false);
                m_queryService.AddTriplet(game, m_gamePredicates[platforms], developer, false);
                m_queryService.AddTriplet(game, m_gamePredicates[platforms], engine, false);
                m_queryService.AddTriplet(game, m_gamePredicates[platforms], genre, false);
                m_queryService.AddTriplet(game, m_gamePredicates[platforms], releaseDate, false);
                m_queryService.AddTriplet(game, m_gamePredicates[platforms], series, false);

                m_queryService.AddFilter((builder) => builder.Lang(builder.Variable(description)) == m_queryService.GetLang());
                m_queryService.Union();


                m_queryService.SetQueryLimit(1);
                m_queryService.SetQueryOffset(number);

                var result = GetResult(resType);

                m_queryService.Clear();

                return result;
            }
        }

    }
}
