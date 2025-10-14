namespace Finance.Tracking.WebApi;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApplicationPipeline(this WebApplication app)
    {
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerWithUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors();
        app.MapControllers();

        return app;
    }

    private static void UseSwaggerWithUI(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Finance API v1");
            c.RoutePrefix = "swagger";
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            c.InjectJavascript("/swagger/custom-swagger.js");
        });
    }
}
