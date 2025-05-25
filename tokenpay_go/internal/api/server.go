package api

import (
	"database/sql"
	"log"
	"net/http"

	"tokenpay_go/internal/config"
	"tokenpay_go/internal/ordermanager"
	"tokenpay_go/internal/paymentprocessor"
)

// StartServer initializes and starts the HTTP server.
func StartServer(addr string, db *sql.DB, appSettings *config.AppSettings) error {
	// Create a PaymentProcessor instance
	pp := paymentprocessor.NewPaymentProcessor(db, appSettings)

	// Create an OrderManager instance, passing the PaymentProcessor
	om := ordermanager.NewOrderManager(db, appSettings, pp)

	// Create an ApiHandler instance, passing the OrderManager
	// The NewApiHandler function is defined in handlers.go
	handler := NewApiHandler(db, appSettings, om)

	// Setup HTTP routes
	// Note: For more complex applications, consider using a router like http.ServeMux or a third-party library.
	http.HandleFunc("/api/createorder", handler.CreateOrderHandler)
	http.HandleFunc("/api/queryorder", handler.QueryOrderHandler) // Changed from /query to /api/queryorder for consistency
	http.HandleFunc("/pay", handler.PayPageHandler)           // Kept /pay as it's user-facing
	http.HandleFunc("/getqrcode", handler.GetQrCodeHandler)   // Kept /getqrcode as it's used by /pay

	log.Printf("Starting server on %s", addr)
	log.Println("Available endpoints:")
	log.Println("  POST /api/createorder")
	log.Println("  GET  /api/queryorder?orderId=...")
	log.Println("  GET  /pay?orderId=...")
	log.Println("  GET  /getqrcode?data=...")

	// Start the HTTP server
	err := http.ListenAndServe(addr, nil)
	if err != nil {
		log.Printf("Error starting server: %v", err)
		return err
	}
	return nil
}
