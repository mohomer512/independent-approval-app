using IndependentApproval.Api.Application.Documents;
using IndependentApproval.Api.Contracts.Documents;
using IndependentApproval.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace IndependentApproval.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DocumentStorageOptions.MultipartBodyLengthLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentStorageOptions.MultipartBodyLengthLimitBytes)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<DocumentResponse>> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await documentService.UploadAsync(request, cancellationToken);

            return CreatedAtRoute(DocumentRoutes.Download, new { id = response.Id }, response);
        }
        catch (DocumentValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return ValidationProblem(ModelState);
        }
        catch (DocumentFileTooLargeException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "The document file is too large.");
        }
        catch (DocumentIdentityUnavailableException)
        {
            return Challenge();
        }
    }

    [HttpGet]
    [ProducesResponseType<DocumentListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentService.ListAsync(page, pageSize, cancellationToken);
        }
        catch (DocumentValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return ValidationProblem(ModelState);
        }
        catch (DocumentIdentityUnavailableException)
        {
            return Challenge();
        }
    }

    [HttpGet("{id:guid}/download", Name = DocumentRoutes.Download)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var download = await documentService.GetDownloadAsync(id, cancellationToken);

            if (download is null)
            {
                return NotFound();
            }

            Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

            return File(
                download.Content,
                download.ContentType,
                download.OriginalFileName,
                enableRangeProcessing: true);
        }
        catch (DocumentIdentityUnavailableException)
        {
            return Challenge();
        }
    }
}
