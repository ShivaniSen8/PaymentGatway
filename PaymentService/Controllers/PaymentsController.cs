using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs;
using PaymentService.Exceptions;
using PaymentService.Gateways;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment(
        CreatePaymentRequest request)
    {
        try
        {
            var payment = await _paymentService.CreatePaymentAsync(request);
            return CreatedAtAction(
                nameof(GetPayment),
                new { id = payment.PaymentId },
                payment);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PaymentProviderException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: ex.Message);
        }
        catch (OrderServiceException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        try
        {
            var payment = await _paymentService.GetPaymentAsync(id);
            return payment == null ? NotFound() : Ok(payment);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (OrderServiceException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> VerifyPayment(
        Guid id,
        PaymentGatewayVerificationRequest request)
    {
        try
        {
            var payment = await _paymentService.VerifyPaymentAsync(id, request);
            return Ok(payment);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PaymentVerificationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (OrderServiceException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: ex.Message);
        }
        catch (PaymentProviderException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
