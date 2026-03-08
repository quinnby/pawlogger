using CarCareTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace CarCareTracker.Filter
{
    public class QueryParamFilter : ActionFilterAttribute
    {
        private readonly string[] _queryParams;
        private readonly ILogger<QueryParamFilter> _logger;
        private static readonly Dictionary<string, string[]> QueryParamAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "vehicleId", new [] { "petProfileId" } }
        };

        public QueryParamFilter(string[] queryParams, ILogger<QueryParamFilter> logger)
        {
            _queryParams = queryParams;
            _logger = logger;
        }
        public override async void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Dictionary<string, string> paramDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "vehicleId", "int" },
                { "autoIncludeEquipment", "bool" }
            };
            // Query values are the safest compatibility source and also support GET aliases.
            foreach (string queryParam in _queryParams)
            {
                if (queryParam.Equals("vehicleId", StringComparison.OrdinalIgnoreCase) &&
                    HasConflictingAliasInQuery(filterContext.HttpContext.Request.Query, queryParam, out var aliasKey))
                {
                    _logger.LogWarning("Phase8 id alias mismatch in query: {LegacyKey} and {AliasKey}", queryParam, aliasKey);
                    filterContext.Result = new BadRequestObjectResult(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
                    return;
                }
                if (filterContext.ActionArguments.ContainsKey(queryParam))
                {
                    continue;
                }
                if (TryReadRequestValue(filterContext, queryParam, out var queryValue, out var querySource))
                {
                    if (TryConvertToTypedArgument(queryParam, queryValue, paramDictionary, out var typedQueryValue))
                    {
                        filterContext.ActionArguments[queryParam] = typedQueryValue;
                        if (!queryParam.Equals(querySource, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Phase7 compatibility alias used in query: {AliasKey} -> {LegacyKey}", querySource, queryParam);
                        }
                    }
                }
            }

            if (_queryParams.Any(x => !filterContext.ActionArguments.ContainsKey(x)))
            {
                filterContext.HttpContext.Request.Body.Position = 0;
                var reader = new StreamReader(filterContext.HttpContext.Request.Body, Encoding.UTF8);
                var rawMessage = await reader.ReadToEndAsync();
                filterContext.HttpContext.Request.Body.Position = 0;
                if (!string.IsNullOrWhiteSpace(rawMessage))
                {
                    Dictionary<string, JsonElement> dynamicDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rawMessage) ?? new Dictionary<string, JsonElement>();
                    foreach (string queryParam in _queryParams)
                    {
                        if (queryParam.Equals("vehicleId", StringComparison.OrdinalIgnoreCase) &&
                            HasConflictingAliasInBody(dynamicDictionary, queryParam, out var aliasKey))
                        {
                            _logger.LogWarning("Phase8 id alias mismatch in request body: {LegacyKey} and {AliasKey}", queryParam, aliasKey);
                            filterContext.Result = new BadRequestObjectResult(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
                            return;
                        }
                        if (filterContext.ActionArguments.ContainsKey(queryParam))
                        {
                            continue;
                        }

                        if (!TryReadBodyValue(dynamicDictionary, queryParam, out var bodyValue, out var bodySource))
                        {
                            continue;
                        }

                        if (TryConvertToTypedArgument(queryParam, bodyValue, paramDictionary, out var typedBodyValue))
                        {
                            filterContext.ActionArguments[queryParam] = typedBodyValue;
                            if (!queryParam.Equals(bodySource, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Phase7 compatibility alias used in request body: {AliasKey} -> {LegacyKey}", bodySource, queryParam);
                            }
                        }
                    }
                }
            }
        }

        private static bool TryConvertToTypedArgument(string queryParam, string rawValue, Dictionary<string, string> paramDictionary, out object typedValue)
        {
            typedValue = rawValue;
            if (!paramDictionary.TryGetValue(queryParam, out var queryParamType) || string.IsNullOrWhiteSpace(queryParamType))
            {
                return true;
            }
            if (queryParamType == "int")
            {
                if (int.TryParse(rawValue, out int parsedInt))
                {
                    typedValue = parsedInt;
                    return true;
                }
                return false;
            }
            if (queryParamType == "bool")
            {
                if (bool.TryParse(rawValue, out bool parsedBool))
                {
                    typedValue = parsedBool;
                    return true;
                }
                return false;
            }
            typedValue = rawValue;
            return true;
        }

        private static bool TryReadRequestValue(ActionExecutingContext filterContext, string queryParam, out string value, out string source)
        {
            value = string.Empty;
            source = queryParam;
            var query = filterContext.HttpContext.Request.Query;
            if (query.TryGetValue(queryParam, out StringValues directValue) && !StringValues.IsNullOrEmpty(directValue))
            {
                value = directValue.ToString();
                source = queryParam;
                return true;
            }
            if (!QueryParamAliases.TryGetValue(queryParam, out var aliases))
            {
                return false;
            }
            foreach (var alias in aliases)
            {
                if (query.TryGetValue(alias, out StringValues aliasValue) && !StringValues.IsNullOrEmpty(aliasValue))
                {
                    value = aliasValue.ToString();
                    source = alias;
                    return true;
                }
            }
            return false;
        }

        private static bool TryReadBodyValue(Dictionary<string, JsonElement> body, string queryParam, out string value, out string source)
        {
            value = string.Empty;
            source = queryParam;
            foreach (var entry in body)
            {
                if (entry.Key.Equals(queryParam, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.Value.ToString();
                    source = entry.Key;
                    return true;
                }
            }
            if (!QueryParamAliases.TryGetValue(queryParam, out var aliases))
            {
                return false;
            }
            foreach (var alias in aliases)
            {
                foreach (var entry in body)
                {
                    if (entry.Key.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        value = entry.Value.ToString();
                        source = entry.Key;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HasConflictingAliasInQuery(IQueryCollection query, string queryParam, out string aliasKey)
        {
            aliasKey = string.Empty;
            if (!query.TryGetValue(queryParam, out var directValue) || StringValues.IsNullOrEmpty(directValue))
            {
                return false;
            }
            if (!QueryParamAliases.TryGetValue(queryParam, out var aliases))
            {
                return false;
            }
            if (!int.TryParse(directValue.ToString(), out var parsedDirectValue))
            {
                return false;
            }

            foreach (var alias in aliases)
            {
                if (!query.TryGetValue(alias, out var aliasValue) || StringValues.IsNullOrEmpty(aliasValue))
                {
                    continue;
                }
                if (int.TryParse(aliasValue.ToString(), out var parsedAliasValue) && parsedAliasValue != parsedDirectValue)
                {
                    aliasKey = alias;
                    return true;
                }
            }
            return false;
        }

        private static bool HasConflictingAliasInBody(Dictionary<string, JsonElement> body, string queryParam, out string aliasKey)
        {
            aliasKey = string.Empty;
            if (!TryReadBodyKeyCaseInsensitive(body, queryParam, out var directValue))
            {
                return false;
            }
            if (!QueryParamAliases.TryGetValue(queryParam, out var aliases))
            {
                return false;
            }
            if (!int.TryParse(directValue, out var parsedDirectValue))
            {
                return false;
            }

            foreach (var alias in aliases)
            {
                if (!TryReadBodyKeyCaseInsensitive(body, alias, out var aliasValue))
                {
                    continue;
                }
                if (int.TryParse(aliasValue, out var parsedAliasValue) && parsedAliasValue != parsedDirectValue)
                {
                    aliasKey = alias;
                    return true;
                }
            }
            return false;
        }

        private static bool TryReadBodyKeyCaseInsensitive(Dictionary<string, JsonElement> body, string key, out string value)
        {
            value = string.Empty;
            foreach (var entry in body)
            {
                if (entry.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.Value.ToString();
                    return true;
                }
            }
            return false;
        }
    }
}
