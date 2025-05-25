package ordermanager

import (
	"database/sql"
	"fmt"
	"time"

	"github.com/google/uuid"
	"tokenpay_go/internal/config"
	"tokenpay_go/internal/database"
	"tokenpay_go/internal/models"
	"tokenpay_go/internal/paymentprocessor"
)

// OrderManager handles business logic related to orders.
type OrderManager struct {
	DB          *sql.DB
	AppSettings *config.AppSettings
	PP          *paymentprocessor.PaymentProcessor
}

// NewOrderManager creates a new OrderManager.
func NewOrderManager(db *sql.DB, appSettings *config.AppSettings, pp *paymentprocessor.PaymentProcessor) *OrderManager {
	return &OrderManager{DB: db, AppSettings: appSettings, PP: pp}
}

// CreateNewOrder creates a new token order based on the view model.
func (om *OrderManager) CreateNewOrder(createViewModel *models.CreateOrderViewModel) (*models.TokenOrder, error) {
	// Validate Currency
	rate, ok := om.AppSettings.Rate[createViewModel.Currency]
	if !ok {
		return nil, fmt.Errorf("currency %s is not supported", createViewModel.Currency)
	}
	if rate <= 0 {
		// Consider if a default rate should be used or if this is a hard error.
		// For now, treating as an error if rate is invalid.
		return nil, fmt.Errorf("invalid rate configuration for currency %s: rate is %f", createViewModel.Currency, rate)
	}

	// Validate ActualAmount
	if createViewModel.ActualAmount <= 0 {
		return nil, fmt.Errorf("actualAmount must be greater than 0, got %f", createViewModel.ActualAmount)
	}

	// Calculate cryptoAmount
	cryptoAmount := createViewModel.ActualAmount / rate

	// Populate initial TokenOrder fields (ID is needed for address generation context)
	tokenOrder := models.TokenOrder{
		ID:           uuid.New().String(),
		OutOrderID:   createViewModel.OutOrderID,
		OrderUserKey: createViewModel.OrderUserKey,
		ActualAmount: createViewModel.ActualAmount,
		Currency:     createViewModel.Currency,
		Amount:       cryptoAmount,
		// ToAddress will be set by PaymentProcessor
		Status:          models.OrderStatusPending,
		NotifyURL:       createViewModel.NotifyURL,
		RedirectURL:     createViewModel.RedirectURL,
		PassThroughInfo: createViewModel.PassThroughInfo,
		CreateTime:      time.Now(),
		CallbackNum:     0,
		CallbackConfirm: false,
	}

	// Determine ToAddress using PaymentProcessor
	toAddress, err := om.PP.GeneratePaymentAddress(createViewModel.Currency, tokenOrder.ID)
	if err != nil {
		return nil, fmt.Errorf("failed to generate payment address: %w", err)
	}
	tokenOrder.ToAddress = toAddress

	// Save the order to the database
	_, err = database.CreateTokenOrder(om.DB, &tokenOrder)
	if err != nil {
		return nil, fmt.Errorf("failed to save order to database: %w", err)
	}

	return &tokenOrder, nil
}

// GetOrderByID retrieves an order by its ID.
func (om *OrderManager) GetOrderByID(orderID string) (*models.TokenOrder, error) {
	order, err := database.GetTokenOrderByID(om.DB, orderID)
	if err != nil {
		return nil, fmt.Errorf("failed to get order by ID from database: %w", err)
	}
	// database.GetTokenOrderByID returns nil, nil if not found.
	// We might want to propagate this as a specific "not found" error type
	// or let the caller handle nil order.
	return order, nil
}

// UpdateOrderStatus updates the status and payment details of an existing order.
func (om *OrderManager) UpdateOrderStatus(orderID string, newStatus string, blockTxID *string, payAmount *float64, fromAddress *string, payTime *time.Time) error {
	order, err := database.GetTokenOrderByID(om.DB, orderID)
	if err != nil {
		return fmt.Errorf("failed to retrieve order %s for update: %w", orderID, err)
	}
	if order == nil {
		return fmt.Errorf("order with ID %s not found for update", orderID)
	}

	order.Status = newStatus
	if blockTxID != nil {
		order.BlockTransactionID = blockTxID
	}
	if payAmount != nil {
		order.PayAmount = payAmount
	}
	if fromAddress != nil {
		order.FromAddress = fromAddress
	}
	if payTime != nil {
		order.PayTime = payTime
	}
	// Potentially update LastNotifyTime or other fields related to status change
	// order.LastNotifyTime = timePtr(time.Now()) // If notification is attempted immediately

	err = database.UpdateTokenOrder(om.DB, order)
	if err != nil {
		return fmt.Errorf("failed to update order %s in database: %w", orderID, err)
	}
	return nil
}

// Helper functions (if needed, like timePtr from handlers, can be moved to a common internal package)
// func timePtr(t time.Time) *time.Time {
// 	return &t
// }
// func float64Ptr(f float64) *float64 {
// 	return &f
// }
// func stringPtr(s string) *string {
//  return &s
// }
