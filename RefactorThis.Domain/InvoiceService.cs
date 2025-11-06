using System;
using System.Collections.Generic;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
	public class InvoiceService
	{
		private const decimal CommercialTaxRate = 0.14m;
		private readonly IInvoiceRepository _invoiceRepository;

		public InvoiceService( IInvoiceRepository invoiceRepository )
		{
			_invoiceRepository = invoiceRepository;
		}

		public string ProcessPayment( Payment payment )
		{
			var invoice = _invoiceRepository.GetInvoice( payment.Reference );
			ValidateInvoiceExists( invoice );
			ValidateInvoiceWithZeroAmount( invoice );
			if ( invoice.Amount == 0 )
			{
				return "no payment needed";
			}
			var hasExistingPayments = HasExistingPayments( invoice );
			if ( hasExistingPayments && IsInvoiceFullyPaid( invoice ) )
			{
				return "invoice was already fully paid";
			}
			if ( hasExistingPayments && IsPaymentExceedingRemaining( invoice, payment ) )
			{
				return "the payment is greater than the partial amount remaining";
			}
			if ( !hasExistingPayments && payment.Amount > invoice.Amount )
			{
				return "the payment is greater than the invoice amount";
			}
			var message = ApplyPaymentToInvoice( invoice, payment, hasExistingPayments );
			_invoiceRepository.SaveInvoice( invoice );
			return message;
		}

		private void ValidateInvoiceExists( Invoice invoice )
		{
			if ( invoice == null )
			{
				throw new InvalidOperationException( "There is no invoice matching this payment" );
			}
		}

		private void ValidateInvoiceWithZeroAmount( Invoice invoice )
		{
			if ( invoice.Amount == 0 && HasExistingPayments( invoice ) )
			{
				throw new InvalidOperationException( "The invoice is in an invalid state, it has an amount of 0 and it has payments." );
			}
		}

		private bool HasExistingPayments( Invoice invoice )
		{
			return invoice.Payments != null && invoice.Payments.Any();
		}

		private bool IsInvoiceFullyPaid( Invoice invoice )
		{
			var totalPaid = invoice.Payments.Sum( x => x.Amount );
			return totalPaid != 0 && invoice.Amount == totalPaid;
		}

		private bool IsPaymentExceedingRemaining( Invoice invoice, Payment payment )
		{
			var remaining = invoice.Amount - invoice.AmountPaid;
			var totalPaid = invoice.Payments.Sum( x => x.Amount );
			return totalPaid != 0 && payment.Amount > remaining;
		}

		private string ApplyPaymentToInvoice( Invoice invoice, Payment payment, bool hasExistingPayments )
		{
			var remaining = invoice.Amount - invoice.AmountPaid;
			var isFullPayment = hasExistingPayments && remaining == payment.Amount || !hasExistingPayments && invoice.Amount == payment.Amount;
			if ( invoice.Payments == null )
			{
				invoice.Payments = new List<Payment>();
			}
			if ( hasExistingPayments )
			{
				invoice.AmountPaid += payment.Amount;
			}
			else
			{
				invoice.AmountPaid = payment.Amount;
			}
			invoice.TaxAmount = CalculateUpdatedTax( invoice, payment, hasExistingPayments );
			invoice.Payments.Add( payment );
			return GetPaymentMessage( hasExistingPayments, isFullPayment );
		}

		private decimal CalculateUpdatedTax( Invoice invoice, Payment payment, bool hasExistingPayments )
		{
			if ( invoice.Type == InvoiceType.Commercial )
			{
				var existingTax = hasExistingPayments ? invoice.TaxAmount : 0m;
				var taxToAdd = payment.Amount * CommercialTaxRate;
				return existingTax + taxToAdd;
			}
			return hasExistingPayments ? invoice.TaxAmount : 0m;
		}

		private string GetPaymentMessage( bool hasExistingPayments, bool isFullPayment )
		{
			if ( hasExistingPayments )
			{
				return isFullPayment ? "final partial payment received, invoice is now fully paid" : "another partial payment received, still not fully paid";
			}
			return isFullPayment ? "invoice is now fully paid" : "invoice is now partially paid";
		}
	}
}