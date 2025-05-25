package database

import (
	"database/sql"
	"fmt"
	"log"
	"time"

	"github.com/google/uuid"
	_ "github.com/mattn/go-sqlite3"
	"tokenpay_go/internal/models"
)

// InitDB initializes and returns a database connection.
func InitDB(filepath string) (*sql.DB, error) {
	db, err := sql.Open("sqlite3", filepath)
	if err != nil {
		return nil, fmt.Errorf("failed to open database: %w", err)
	}
	if err = db.Ping(); err != nil {
		db.Close()
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}
	log.Println("Database connection established.")
	return db, nil
}

// CreateTable creates a table if it doesn't exist.
func CreateTable(db *sql.DB, tableName string, query string) error {
	var name string
	err := db.QueryRow("SELECT name FROM sqlite_master WHERE type='table' AND name=?", tableName).Scan(&name)
	if err != nil && err != sql.ErrNoRows {
		return fmt.Errorf("failed to check if table %s exists: %w", tableName, err)
	}
	if name == tableName {
		log.Printf("Table %s already exists.\n", tableName)
		return nil
	}

	_, err = db.Exec(query)
	if err != nil {
		return fmt.Errorf("failed to create table %s: %w", tableName, err)
	}
	log.Printf("Table %s created successfully.\n", tableName)
	return nil
}

// CreateSchema creates the database schema.
func CreateSchema(db *sql.DB) error {
	tokenOrderTableQuery := `
	CREATE TABLE TokenOrder (
		id TEXT PRIMARY KEY,
		out_order_id TEXT,
		order_user_key TEXT,
		block_transaction_id TEXT,
		pay_time DATETIME,
		pay_amount REAL,
		is_dynamic_amount INTEGER,
		from_address TEXT,
		actual_amount REAL,
		currency TEXT,
		amount REAL,
		to_address TEXT,
		status TEXT,
		pass_through_info TEXT,
		notify_url TEXT,
		redirect_url TEXT,
		callback_num INTEGER,
		callback_confirm INTEGER,
		last_notify_time DATETIME,
		create_time DATETIME
	);`
	if err := CreateTable(db, "TokenOrder", tokenOrderTableQuery); err != nil {
		return err
	}

	tokenRateTableQuery := `
	CREATE TABLE TokenRate (
		id TEXT PRIMARY KEY,
		currency TEXT,
		fiat_currency TEXT,
		rate REAL,
		last_update_time DATETIME
	);`
	if err := CreateTable(db, "TokenRate", tokenRateTableQuery); err != nil {
		return err
	}

	tokenWalletTableQuery := `
	CREATE TABLE TokenWallet (
		id TEXT PRIMARY KEY,
		address TEXT,
		key TEXT,
		chain_type TEXT,
		balance REAL,
		usdt_balance REAL,
		last_check_time DATETIME
	);`
	if err := CreateTable(db, "TokenWallet", tokenWalletTableQuery); err != nil {
		return err
	}

	log.Println("Database schema created/verified.")
	return nil
}

// CreateTokenOrder inserts a new TokenOrder into the database.
func CreateTokenOrder(db *sql.DB, order *models.TokenOrder) (string, error) {
	if order.ID == "" {
		order.ID = uuid.New().String()
	}
	if order.CreateTime.IsZero() {
		order.CreateTime = time.Now()
	}

	stmt, err := db.Prepare(`
		INSERT INTO TokenOrder (
			id, out_order_id, order_user_key, block_transaction_id, pay_time, 
			pay_amount, is_dynamic_amount, from_address, actual_amount, currency, 
			amount, to_address, status, pass_through_info, notify_url, 
			redirect_url, callback_num, callback_confirm, last_notify_time, create_time
		) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
	`)
	if err != nil {
		return "", fmt.Errorf("failed to prepare insert statement for TokenOrder: %w", err)
	}
	defer stmt.Close()

	_, err = stmt.Exec(
		order.ID, order.OutOrderID, order.OrderUserKey, order.BlockTransactionID, order.PayTime,
		order.PayAmount, order.IsDynamicAmount, order.FromAddress, order.ActualAmount, order.Currency,
		order.Amount, order.ToAddress, order.Status, order.PassThroughInfo, order.NotifyURL,
		order.RedirectURL, order.CallbackNum, order.CallbackConfirm, order.LastNotifyTime, order.CreateTime,
	)
	if err != nil {
		return "", fmt.Errorf("failed to execute insert statement for TokenOrder: %w", err)
	}
	return order.ID, nil
}

// GetTokenOrderByID retrieves a TokenOrder from the database by its ID.
func GetTokenOrderByID(db *sql.DB, id string) (*models.TokenOrder, error) {
	stmt, err := db.Prepare(`
		SELECT 
			id, out_order_id, order_user_key, block_transaction_id, pay_time, 
			pay_amount, is_dynamic_amount, from_address, actual_amount, currency, 
			amount, to_address, status, pass_through_info, notify_url, 
			redirect_url, callback_num, callback_confirm, last_notify_time, create_time
		FROM TokenOrder WHERE id = ?
	`)
	if err != nil {
		return nil, fmt.Errorf("failed to prepare select statement for TokenOrder: %w", err)
	}
	defer stmt.Close()

	order := &models.TokenOrder{}
	err = stmt.QueryRow(id).Scan(
		&order.ID, &order.OutOrderID, &order.OrderUserKey, &order.BlockTransactionID, &order.PayTime,
		&order.PayAmount, &order.IsDynamicAmount, &order.FromAddress, &order.ActualAmount, &order.Currency,
		&order.Amount, &order.ToAddress, &order.Status, &order.PassThroughInfo, &order.NotifyURL,
		&order.RedirectURL, &order.CallbackNum, &order.CallbackConfirm, &order.LastNotifyTime, &order.CreateTime,
	)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, nil // Or a custom not found error
		}
		return nil, fmt.Errorf("failed to execute select statement for TokenOrder: %w", err)
	}
	return order, nil
}

// UpdateTokenOrder updates an existing TokenOrder in the database.
func UpdateTokenOrder(db *sql.DB, order *models.TokenOrder) error {
	stmt, err := db.Prepare(`
		UPDATE TokenOrder SET 
			out_order_id = ?, 
			order_user_key = ?, 
			block_transaction_id = ?, 
			pay_time = ?, 
			pay_amount = ?, 
			is_dynamic_amount = ?, 
			from_address = ?, 
			actual_amount = ?, 
			currency = ?, 
			amount = ?, 
			to_address = ?, 
			status = ?, 
			pass_through_info = ?, 
			notify_url = ?, 
			redirect_url = ?, 
			callback_num = ?, 
			callback_confirm = ?, 
			last_notify_time = ?, 
			create_time = ?  -- Though create_time typically doesn't change, it's included for completeness of the model
		WHERE id = ?
	`)
	if err != nil {
		return fmt.Errorf("failed to prepare update statement for TokenOrder: %w", err)
	}
	defer stmt.Close()

	_, err = stmt.Exec(
		order.OutOrderID, order.OrderUserKey, order.BlockTransactionID, order.PayTime,
		order.PayAmount, order.IsDynamicAmount, order.FromAddress, order.ActualAmount, order.Currency,
		order.Amount, order.ToAddress, order.Status, order.PassThroughInfo, order.NotifyURL,
		order.RedirectURL, order.CallbackNum, order.CallbackConfirm, order.LastNotifyTime, order.CreateTime,
		order.ID,
	)
	if err != nil {
		return fmt.Errorf("failed to execute update statement for TokenOrder: %w", err)
	}
	return nil
}
