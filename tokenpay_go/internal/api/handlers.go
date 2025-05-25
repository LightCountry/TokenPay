package api

import (
	"bytes"
	"database/sql"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"time"

	"github.com/google/uuid"
	"github.com/skip2/go-qrcode"
	"tokenpay_go/internal/config"
	"tokenpay_go/internal/models"
	"tokenpay_go/internal/ordermanager"
	// "tokenpay_go/internal/database" // Will be used when interacting with DB
)

// ApiHandler holds dependencies for HTTP handlers.
type ApiHandler struct {
	DB          *sql.DB
	AppSettings *config.AppSettings
	OM          *ordermanager.OrderManager
}

// NewApiHandler creates a new ApiHandler.
func NewApiHandler(db *sql.DB, appSettings *config.AppSettings, om *ordermanager.OrderManager) *ApiHandler {
	return &ApiHandler{DB: db, AppSettings: appSettings, OM: om}
}

// CreateOrderHandler handles order creation requests.
func (ah *ApiHandler) CreateOrderHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var createOrderReq models.CreateOrderViewModel
	err := json.NewDecoder(r.Body).Decode(&createOrderReq)
	if err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	// Placeholder: Basic validation
	if createOrderReq.OutOrderID == "" {
		http.Error(w, "OutOrderId is required", http.StatusBadRequest)
		return
	}

	log.Printf("Received CreateOrder request: %+v", createOrderReq)

	order, err := ah.OM.CreateNewOrder(&createOrderReq)
	if err != nil {
		log.Printf("Error creating new order: %v", err)
		http.Error(w, fmt.Sprintf("Failed to create order: %s", err.Error()), http.StatusInternalServerError)
		return
	}

	paymentURL := fmt.Sprintf("%s/pay?orderId=%s", ah.AppSettings.WebSiteUrl, order.ID)

	respData := models.ReturnData{
		Success: true,
		Data: map[string]interface{}{
			"orderId":     order.ID,
			"paymentUrl":  paymentURL,
			"outOrderId":  order.OutOrderID,
			"actualAmount": order.ActualAmount,
			"currency":    order.Currency,
			"status":      order.Status,
			"createTime":  order.CreateTime.Format(time.RFC3339),
			// Add other fields from 'order' if needed in the response
		},
	}

	w.Header().Set("Content-Type", "application/json")
	err = json.NewEncoder(w).Encode(respData)
	if err != nil {
		log.Printf("Error encoding response: %v", err)
		http.Error(w, "Failed to encode response", http.StatusInternalServerError)
	}
}

// QueryOrderHandler handles order query requests.
func (ah *ApiHandler) QueryOrderHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	orderID := r.URL.Query().Get("orderId")
	if orderID == "" {
		http.Error(w, "orderId query parameter is required", http.StatusBadRequest)
		return
	}

	log.Printf("Received QueryOrder request for orderId: %s", orderID)

	order, err := ah.OM.GetOrderByID(orderID)
	if err != nil {
		log.Printf("Error fetching order %s: %v", orderID, err)
		// Differentiate between "not found" and other errors if GetOrderByID provides that info
		respData := models.ReturnData{Success: false, Message: strPtr(fmt.Sprintf("Error fetching order: %v", err))}
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusInternalServerError) // Or http.StatusNotFound if applicable
		json.NewEncoder(w).Encode(respData)
		return
	}

	if order == nil {
		respData := models.ReturnData{Success: false, Message: strPtr("Order not found")}
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusNotFound)
		json.NewEncoder(w).Encode(respData)
		return
	}

	respData := models.ReturnData{
		Success: true,
		Data:    order,
	}

	w.Header().Set("Content-Type", "application/json")
	err := json.NewEncoder(w).Encode(respData)
	if err != nil {
		log.Printf("Error encoding response: %v", err)
		http.Error(w, "Failed to encode response", http.StatusInternalServerError)
	}
}

// PayPageHandler serves a simple HTML page for payment.
func (ah *ApiHandler) PayPageHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	orderID := r.URL.Query().Get("orderId")
	if orderID == "" {
		http.Error(w, "orderId query parameter is required", http.StatusBadRequest)
		return
	}

	log.Printf("Received PayPage request for orderId: %s", orderID)

	order, err := ah.OM.GetOrderByID(orderID)
	if err != nil {
		log.Printf("Error fetching order %s for pay page: %v", orderID, err)
		http.Error(w, "Error retrieving order details.", http.StatusInternalServerError)
		return
	}
	if order == nil {
		log.Printf("Order %s not found for pay page.", orderID)
		http.Error(w, "Order not found.", http.StatusNotFound)
		return
	}

	// Use order details to populate the page
	// The QR data could be just the address, or a more complex payment URI if applicable
	// (e.g., "ethereum:address?value=amount&token=token_symbol")
	qrData := order.ToAddress // Default to just the address
	// Example for a more specific URI if needed for certain currencies/chains:
	// if order.Currency == "ETH" || (strings.HasPrefix(order.Currency, "ERC20_")) {
	// qrData = fmt.Sprintf("ethereum:%s?value=%f", order.ToAddress, order.Amount)
	// } else if order.Currency == "TRX" || order.Currency == "TRC20_USDT" {
	// qrData = fmt.Sprintf("tron:%s?amount=%f", order.ToAddress, order.Amount)
	// }


	htmlContent := `
	<!DOCTYPE html>
	<html lang="en">
	<head>
		<meta charset="UTF-8">
		<meta name="viewport" content="width=device-width, initial-scale=1.0">
		<title>Pay Order</title>
		<style>
			body { font-family: Arial, sans-serif; margin: 20px; text-align: center; }
			.container { max-width: 400px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }
			h1 { color: #333; }
			p { color: #555; font-size: 1.1em; }
			.qr-code { margin-top: 20px; }
			.address { word-wrap: break-word; background-color: #f0f0f0; padding: 10px; border-radius: 4px; margin-top:10px;}
		</style>
	</head>
	<body>
		<div class="container">
			<h1>Pay Order: %s</h1>
			<p>Out Order ID: %s</p>
			<p>Amount: %.8f %s</p> <!-- Display more precision for crypto amounts -->
			<p>To Address:</p>
			<div class="address">%s</div>
			<div class="qr-code">
				<img src="/getqrcode?data=%s" alt="QR Code for Payment" />
			</div>
			<p style="font-size:0.9em; color: #777; margin-top:20px;">Scan the QR code or copy the address to pay.</p>
			<p style="font-size:0.8em; color: #aaa; margin-top:10px;">Order ID: %s</p>
		</div>
	</body>
	</html>`

	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	// Use order.ID for display, order.Amount for amount, order.Currency, order.ToAddress
	_, err = fmt.Fprintf(w, htmlContent, order.ID, order.OutOrderID, order.Amount, order.Currency, order.ToAddress, qrData, order.ID)
	if err != nil {
		log.Printf("Error writing PayPage response for order %s: %v", orderID, err)
	}
}

// GetQrCodeHandler generates and serves a QR code image.
func (ah *ApiHandler) GetQrCodeHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	data := r.URL.Query().Get("data")
	if data == "" {
		http.Error(w, "data query parameter is required for QR code generation", http.StatusBadRequest)
		return
	}

	log.Printf("Generating QR code for data: %s", data)

	var png []byte
	png, err := qrcode.Encode(data, qrcode.Medium, 256) // Medium redundancy, 256x256 pixels
	if err != nil {
		log.Printf("Error generating QR code: %v", err)
		http.Error(w, "Failed to generate QR code", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "image/png")
	_, err = w.Write(png)
	if err != nil {
		log.Printf("Error writing QR code image: %v", err)
	}
}

// Helper to get a pointer to a string
func strPtr(s string) *string {
	return &s
}

// Helper to get a pointer to a float64
func float64Ptr(f float64) *float64 {
	return &f
}

// Helper to get a pointer to a time.Time
func timePtr(t time.Time) *time.Time {
	return &t
}
