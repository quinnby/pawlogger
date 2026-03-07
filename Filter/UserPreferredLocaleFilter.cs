using CarCareTracker.Helper;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;

namespace CarCareTracker.Filter
{
    public class UserPreferredLocaleFilter : IActionFilter
    {
        private readonly IConfigHelper _config;
        private readonly ILogger<UserPreferredLocaleFilter> _logger;

        public UserPreferredLocaleFilter(IConfigHelper config, ILogger<UserPreferredLocaleFilter> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userConfig = _config.GetUserConfig(context.HttpContext.User);
            if (string.IsNullOrWhiteSpace(userConfig.PreferredLocale))
            {
                return;
            }

            try
            {
                var userCulture = new CultureInfo(userConfig.PreferredLocale.Replace('_', '-'));
                CultureInfo.CurrentCulture = userCulture;
                CultureInfo.CurrentUICulture = userCulture;
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invalid PreferredLocale {PreferredLocale} for user.", userConfig.PreferredLocale);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
