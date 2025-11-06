using System;
using System.Collections.Generic;
using NUnit.Framework;
using RefactorThis.Persistence;

namespace RefactorThis.Domain.Tests
{
	[TestFixture]
	public class InvoicePaymentProcessorTests
	{
		private InvoiceRepository _repo;
		private InvoiceService _paymentProcessor;

		[SetUp]
		public void Setup()
		{
			_repo = new InvoiceRepository();
			_paymentProcessor = new InvoiceService(_repo);
		}

		[Test]
		public void ProcessPayment_Should_ThrowException_When_NoInvoiceFoundForPaymentReference()
		{
			var payment = new Payment { Reference = "INV-999" };

			var ex = Assert.Throws<InvalidOperationException>(() => _paymentProcessor.ProcessPayment(payment));

			Assert.AreEqual("There is no invoice matching this payment", ex.Message);
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_NoPaymentNeeded()
		{
			var invoice = new Invoice
			{
				Reference = "INV-001",
				Amount = 0,
				AmountPaid = 0,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-001" };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("no payment needed", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_InvoiceAlreadyFullyPaid()
		{
			var invoice = new Invoice
			{
				Reference = "INV-002",
				Amount = 10,
				AmountPaid = 10,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>
				{
					new Payment { Amount = 10 }
				}
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-002", Amount = 5 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice was already fully paid", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_PartialPaymentExistsAndAmountPaidExceedsAmountDue()
		{
			var invoice = new Invoice
			{
				Reference = "INV-003",
				Amount = 10,
				AmountPaid = 5,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>
				{
					new Payment { Amount = 5 }
				}
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-003", Amount = 6 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("the payment is greater than the partial amount remaining", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnFailureMessage_When_NoPartialPaymentExistsAndAmountPaidExceedsInvoiceAmount()
		{
			var invoice = new Invoice
			{
				Reference = "INV-004",
				Amount = 5,
				AmountPaid = 0,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-004", Amount = 6 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("the payment is greater than the invoice amount", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnFullyPaidMessage_When_PartialPaymentExistsAndAmountPaidEqualsAmountDue()
		{
			var invoice = new Invoice
			{
				Reference = "INV-005",
				Amount = 10,
				AmountPaid = 5,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>
				{
					new Payment { Amount = 5 }
				}
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-005", Amount = 5 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("final partial payment received, invoice is now fully paid", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnAlreadyPaidMessage_When_InvoiceAlreadyFullyPaidWithSinglePayment()
		{
			var invoice = new Invoice
			{
				Reference = "INV-006",
				Amount = 10,
				AmountPaid = 10,
				Type = InvoiceType.Standard,
				Payments = new List<Payment> { new Payment { Amount = 10 } }
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-006", Amount = 10 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice was already fully paid", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_PartialPaymentExistsAndAmountPaidIsLessThanAmountDue()
		{
			var invoice = new Invoice
			{
				Reference = "INV-007",
				Amount = 10,
				AmountPaid = 5,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>
				{
					new Payment { Amount = 5 }
				}
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-007", Amount = 1 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("another partial payment received, still not fully paid", result);
		}

		[Test]
		public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidIsLessThanInvoiceAmount()
		{
			var invoice = new Invoice
			{
				Reference = "INV-008",
				Amount = 10,
				AmountPaid = 0,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-008", Amount = 1 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice is now partially paid", result);
			Assert.AreEqual(1, invoice.AmountPaid);
			Assert.AreEqual(0, invoice.TaxAmount);
			Assert.AreEqual(1, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_ThrowException_When_ZeroAmountInvoiceHasPayments()
		{
			var invoice = new Invoice
			{
				Reference = "INV-INVALID",
				Amount = 0,
				AmountPaid = 10,
				Type = InvoiceType.Standard,
				Payments = new List<Payment> { new Payment { Amount = 10 } }
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-INVALID", Amount = 5 };

			var ex = Assert.Throws<InvalidOperationException>(() => _paymentProcessor.ProcessPayment(payment));

			Assert.That(ex.Message, Does.Contain("invalid state"));
		}

		[Test]
		public void ProcessPayment_Should_NotCalculateTax_When_StandardInvoiceFullPayment()
		{
			var invoice = new Invoice
			{
				Reference = "INV-STD-FULL",
				Amount = 100,
				AmountPaid = 0,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-STD-FULL", Amount = 100 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice is now fully paid", result);
			Assert.AreEqual(100, invoice.AmountPaid);
			Assert.AreEqual(0, invoice.TaxAmount);
			Assert.AreEqual(1, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_NotCalculateTax_When_StandardInvoicePartialPayment()
		{
			var invoice = new Invoice
			{
				Reference = "INV-STD-PART",
				Amount = 100,
				AmountPaid = 0,
				Type = InvoiceType.Standard,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-STD-PART", Amount = 30 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice is now partially paid", result);
			Assert.AreEqual(30, invoice.AmountPaid);
			Assert.AreEqual(0, invoice.TaxAmount);
			Assert.AreEqual(1, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_NotAccumulateTax_When_StandardInvoiceMultiplePayments()
		{
			var invoice = new Invoice
			{
				Reference = "INV-STD-MULTI",
				Amount = 100,
				AmountPaid = 50,
				TaxAmount = 0,
				Type = InvoiceType.Standard,
				Payments = new List<Payment> { new Payment { Amount = 50 } }
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-STD-MULTI", Amount = 30 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("another partial payment received, still not fully paid", result);
			Assert.AreEqual(80, invoice.AmountPaid);
			Assert.AreEqual(0, invoice.TaxAmount);
			Assert.AreEqual(2, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_CalculateTax_When_CommercialInvoiceFullPayment()
		{
			var invoice = new Invoice
			{
				Reference = "INV-COM-FULL",
				Amount = 100,
				AmountPaid = 0,
				Type = InvoiceType.Commercial,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-COM-FULL", Amount = 100 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice is now fully paid", result);
			Assert.AreEqual(100, invoice.AmountPaid);
			Assert.AreEqual(14.0m, invoice.TaxAmount);
			Assert.AreEqual(1, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_CalculateTax_When_CommercialInvoicePartialPayment()
		{
			var invoice = new Invoice
			{
				Reference = "INV-COM-PART",
				Amount = 100,
				AmountPaid = 0,
				Type = InvoiceType.Commercial,
				Payments = new List<Payment>()
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-COM-PART", Amount = 50 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("invoice is now partially paid", result);
			Assert.AreEqual(50, invoice.AmountPaid);
			Assert.AreEqual(7.0m, invoice.TaxAmount);
			Assert.AreEqual(1, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_AccumulateTax_When_CommercialInvoiceMultiplePayments()
		{
			var invoice = new Invoice
			{
				Reference = "INV-COM-MULTI",
				Amount = 100,
				AmountPaid = 50,
				TaxAmount = 7.0m,
				Type = InvoiceType.Commercial,
				Payments = new List<Payment> { new Payment { Amount = 50 } }
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-COM-MULTI", Amount = 30 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("another partial payment received, still not fully paid", result);
			Assert.AreEqual(80, invoice.AmountPaid);
			Assert.AreEqual(11.2m, invoice.TaxAmount);
			Assert.AreEqual(2, invoice.Payments.Count);
		}

		[Test]
		public void ProcessPayment_Should_CalculateCorrectTax_When_CommercialInvoiceFinalPartialPayment()
		{
			var invoice = new Invoice
			{
				Reference = "INV-COM-FINAL",
				Amount = 100,
				AmountPaid = 70,
				TaxAmount = 9.8m,
				Type = InvoiceType.Commercial,
				Payments = new List<Payment> { new Payment { Amount = 70 } }
			};
			_repo.Add(invoice);

			var payment = new Payment { Reference = "INV-COM-FINAL", Amount = 30 };

			var result = _paymentProcessor.ProcessPayment(payment);

			Assert.AreEqual("final partial payment received, invoice is now fully paid", result);
			Assert.AreEqual(100, invoice.AmountPaid);
			Assert.AreEqual(14.0m, invoice.TaxAmount);
			Assert.AreEqual(2, invoice.Payments.Count);
		}
	}
}