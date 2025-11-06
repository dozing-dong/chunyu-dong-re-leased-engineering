using System;
using System.Collections.Generic;

namespace RefactorThis.Persistence
{
	public class InvoiceRepository : IInvoiceRepository
	{
		private readonly Dictionary<string, Invoice> _invoices = new Dictionary<string, Invoice>();
		private Invoice _lastInvoice;

		public Invoice GetInvoice(string reference)
		{
			if (!string.IsNullOrWhiteSpace(reference) && _invoices.TryGetValue(reference, out var invoice))
			{
				return invoice;
			}
			return _lastInvoice;
		}

		public void SaveInvoice(Invoice invoice)
		{
			if (invoice == null)
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(invoice.Reference))
			{
				_invoices[invoice.Reference] = invoice;
			}
			_lastInvoice = invoice;
		}

		public void Add(Invoice invoice)
		{
			if (invoice == null)
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(invoice.Reference))
			{
				_invoices[invoice.Reference] = invoice;
			}
			_lastInvoice = invoice;
		}
	}
}