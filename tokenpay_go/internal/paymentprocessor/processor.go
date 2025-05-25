package paymentprocessor

import (
	"database/sql"
	"errors"
	"fmt"
	"log"
	"strings"
	"tokenpay_go/internal/config"
	"tokenpay_go/internal/models"
	// "tokenpay_go/internal/database" // If direct DB interaction is needed by PP
)

// PaymentProcessor handles payment validation and address generation.
type PaymentProcessor struct {
	DB          *sql.DB
	AppSettings *config.AppSettings
	// EVMChains   []models.EVMChainConfig // To be added when EVM chain specific logic is needed
}

// NewPaymentProcessor creates a new PaymentProcessor.
func NewPaymentProcessor(db *sql.DB, appSettings *config.AppSettings /*, evmChains []models.EVMChainConfig*/) *PaymentProcessor {
	return &PaymentProcessor{DB: db, AppSettings: appSettings /*, EVMChains: evmChains*/}
}

// GeneratePaymentAddress generates a payment address for a given currency and order.
func (pp *PaymentProcessor) GeneratePaymentAddress(currency string, orderID string) (string, error) {
	currency = strings.ToUpper(currency) // For consistent map lookups

	if pp.AppSettings.UseDynamicAddress {
		// Log a message as per requirement
		fmt.Printf("Dynamic address generation requested for order %s, currency %s. Placeholder: returning static address.\n", orderID, currency)
		// Fallthrough to static address logic for now.
	}

	var chainKey string
	// Simplified mapping:
	// "USDT" or containing "USDT" (e.g. "USDT-TRC20") or "TRX" -> "TRON" key for AppSettings.Address
	// "ETH" or containing "ETH" (e.g. "ETH-ERC20") -> "EVM" key for AppSettings.Address
	// This needs to be robust.
	if strings.Contains(currency, "USDT") || currency == "TRX" {
		chainKey = "TRON"
	} else if strings.Contains(currency, "ETH") { // Assuming "ETH" implies EVM compatible
		chainKey = "EVM"
	} else {
		chainKey = currency // Fallback for other direct currency names like "BTC"
	}
	log.Printf("GeneratePaymentAddress: OrderID %s, InputCurrency %s, MappedChainKey %s", orderID, currency, chainKey)


	addrs, ok := pp.AppSettings.Address[chainKey]
	if ok && len(addrs) > 0 {
		// For static addresses, typically one is chosen.
		log.Printf("Using address %s for currency %s (mapped to chainKey %s) for order %s", addrs[0], currency, chainKey, orderID)
		return addrs[0], nil
	}

	return "", fmt.Errorf("no configured static address found for currency %s (mapped to key %s)", currency, chainKey)
}

// ValidatePayment checks if a transaction notification matches an order.
func (pp *PaymentProcessor) ValidatePayment(order *models.TokenOrder, txNotification *models.TransactionNotification) (bool, error) {
	if order.Status == models.OrderStatusPaid {
		return false, errors.New("order already paid")
	}

	// Address Validation:
	// order.Currency examples: "USDT-TRC20", "TRX", "USDT-ERC20", "ETH"
	// txNotification.Chain examples: "TRX", "ETH"
	// txNotification.TokenSymbol examples: "USDT", "ETH" (native ETH could also be a symbol)
	// txNotification.Address for native coins is the recipient.
	// txNotification.Address for tokens is often the token's contract address.
	// txNotification.TokenReceivingAddress is the actual recipient of the tokens.

	isTokenTransfer := txNotification.TokenSymbol != nil && *txNotification.TokenSymbol != "" && strings.ToUpper(*txNotification.TokenSymbol) != strings.ToUpper(txNotification.Chain)
	// A more explicit check for token: if TokenAddress is not empty and TokenSymbol is not the base coin of the chain
	// e.g. Chain: "ETH", TokenSymbol: "USDT", TokenAddress: "0xdac..." -> isTokenTransfer = true
	// e.g. Chain: "ETH", TokenSymbol: "ETH", TokenAddress: "" (or some native indicator) -> isTokenTransfer = false

	if isTokenTransfer {
		if txNotification.TokenReceivingAddress == "" {
			log.Printf("Order %s: Token transfer identified, but TokenReceivingAddress is missing in notification.", order.ID)
			return false, errors.New("token receiving address missing in notification for token transfer")
		}
		if !strings.EqualFold(txNotification.TokenReceivingAddress, order.ToAddress) {
			log.Printf("Order %s: Token receiving address mismatch. Expected %s, got %s.", order.ID, order.ToAddress, txNotification.TokenReceivingAddress)
			return false, fmt.Errorf("token receiving address mismatch (expected %s, got %s)", order.ToAddress, txNotification.TokenReceivingAddress)
		}
	} else { // Native coin transfer
		if !strings.EqualFold(txNotification.Address, order.ToAddress) {
			log.Printf("Order %s: Native coin address mismatch. Expected %s, got %s.", order.ID, order.ToAddress, txNotification.Address)
			return false, fmt.Errorf("native coin address mismatch (expected %s, got %s)", order.ToAddress, txNotification.Address)
		}
	}
	log.Printf("Order %s: Recipient address validation passed.", order.ID)

	// Amount Validation
	expectedAmount := order.Amount // This is the crypto amount
	var paidAmount float64
	if isTokenTransfer {
		if txNotification.TokenValue == nil {
			log.Printf("Order %s: Token transfer, but TokenValue is missing.", order.ID)
			return false, errors.New("token value missing in notification for token transfer")
		}
		paidAmount = *txNotification.TokenValue
	} else { // Native coin
		paidAmount = txNotification.Value
	}

	amountIsValid := false
	if pp.AppSettings.DynamicAddressConfig.AmountMove {
		var tolerances []float64
		// order.Currency might be "USDT-TRC20", "ETH". Need to map to "USDT", "ETH" for DynamicAddressConfig keys.
		currencyKeyForTolerance := strings.Split(order.Currency, "-")[0] // "USDT-TRC20" -> "USDT"
		currencyKeyForTolerance = strings.ToUpper(currencyKeyForTolerance)

		switch currencyKeyForTolerance {
		case "USDT":
			tolerances = pp.AppSettings.DynamicAddressConfig.USDT
		case "TRX":
			tolerances = pp.AppSettings.DynamicAddressConfig.TRX
		case "ETH":
			tolerances = pp.AppSettings.DynamicAddressConfig.ETH
		default:
			log.Printf("Order %s: No specific amount tolerance configuration for currency key %s (derived from %s). Exact match will be required.", order.ID, currencyKeyForTolerance, order.Currency)
		}

		if len(tolerances) == 2 {
			minAmount := expectedAmount - tolerances[0] // Assuming tolerances[0] is negative deviation
			maxAmount := expectedAmount + tolerances[1] // Assuming tolerances[1] is positive deviation
			if paidAmount >= minAmount && paidAmount <= maxAmount {
				amountIsValid = true
				log.Printf("Order %s: Amount %.8f is within tolerance range [%.8f-%.8f] for expected %.8f.", order.ID, paidAmount, minAmount, maxAmount, expectedAmount)
			} else {
				log.Printf("Order %s: Amount mismatch with tolerance. Expected %.8f (range [%.8f-%.8f]), got %.8f for currency %s.",
					order.ID, expectedAmount, minAmount, maxAmount, paidAmount, order.Currency)
			}
		} else { // No specific tolerance or invalid tolerance config, require "almost equal" or exact.
			// Using a small epsilon for float comparison might be better than exact. For now, exact.
			if paidAmount == expectedAmount {
				amountIsValid = true
				log.Printf("Order %s: Amount matches expected %.8f (no/invalid tolerance config).", order.ID, expectedAmount)
			} else {
				log.Printf("Order %s: Amount mismatch (no/invalid tolerance). Expected %.8f, got %.8f for currency %s.",
					order.ID, expectedAmount, paidAmount, order.Currency)
			}
		}
	} else { // AmountMove is false, require "almost equal" or exact.
		if paidAmount == expectedAmount {
			amountIsValid = true
			log.Printf("Order %s: Amount matches expected %.8f (AmountMove=false).", order.ID, expectedAmount)
		} else {
			log.Printf("Order %s: Amount mismatch (AmountMove=false). Expected %.8f, got %.8f for currency %s.",
				order.ID, expectedAmount, paidAmount, order.Currency)
		}
	}

	if !amountIsValid {
		return false, errors.New("payment amount does not match expected amount")
	}
	log.Printf("Order %s: Amount validation passed.", order.ID)

	// Currency/Token Validation
	// order.Currency format: "SYMBOL-PROTOCOL" e.g. "USDT-TRC20", or just "SYMBOL" for native e.g. "ETH", "TRX"
	parts := strings.Split(order.Currency, "-")
	orderCurrencySymbol := strings.ToUpper(parts[0])
	// var orderCurrencyProtocol string // Not directly used here yet, but could be for contract address matching
	// if len(parts) > 1 {
	// 	orderCurrencyProtocol = strings.ToUpper(parts[1])
	// }

	if isTokenTransfer {
		if txNotification.TokenSymbol == nil {
			log.Printf("Order %s: Token transfer, but TokenSymbol is missing in notification.", order.ID)
			return false, errors.New("token symbol missing in notification for token transfer")
		}
		notifiedTokenSymbol := strings.ToUpper(*txNotification.TokenSymbol)
		if notifiedTokenSymbol != orderCurrencySymbol {
			log.Printf("Order %s: Token symbol mismatch. Expected %s, got %s.", order.ID, orderCurrencySymbol, notifiedTokenSymbol)
			return false, fmt.Errorf("token symbol mismatch (expected %s, got %s)", orderCurrencySymbol, notifiedTokenSymbol)
		}
		// Contract address validation would be here.
		// This requires pp.EVMChains to be populated and a lookup function.
		// For now, this check is conceptual:
		// if txNotification.Chain == "ETH" { // Assuming EVM for this example
		//   chainConfig := findChainConfig(pp.EVMChains, "ETH") // or by a more specific chain ID
		//   if chainConfig != nil {
		//     tokenConfig := findTokenInChainConfig(chainConfig, orderCurrencySymbol)
		//     if tokenConfig != nil {
		//       if !strings.EqualFold(txNotification.TokenAddress, tokenConfig.ContractAddress) {
		//         log.Printf("Order %s: Token contract address mismatch. Expected %s, got %s.", order.ID, tokenConfig.ContractAddress, *txNotification.TokenAddress)
		//         return false, errors.New("token contract address mismatch")
		//       }
		//       log.Printf("Order %s: Token contract address %s matched successfully.", order.ID, *txNotification.TokenAddress)
		//     } else {
		//        log.Printf("Order %s: Token %s not configured for chain %s.", order.ID, orderCurrencySymbol, txNotification.Chain)
		//        return false, errors.New("token not configured for chain")
		//     }
		//   }
		// }
		log.Printf("Order %s: Token symbol %s matched. Contract address validation needs EVMChains integration.", order.ID, notifiedTokenSymbol)
	} else { // Native coin
		// For native coins, txNotification.Chain should match the order's currency symbol (e.g. "ETH", "TRX")
		// This assumes txNotification.Chain is "ETH" for Ethereum native, "TRX" for Tron native etc.
		notifiedChain := strings.ToUpper(txNotification.Chain)
		if notifiedChain != orderCurrencySymbol {
			log.Printf("Order %s: Native coin chain/currency mismatch. Order currency %s, notified chain %s.", order.ID, orderCurrencySymbol, notifiedChain)
			return false, fmt.Errorf("native coin chain/currency mismatch (expected %s, got %s)", orderCurrencySymbol, notifiedChain)
		}
	}
	log.Printf("Order %s: Currency/Token validation passed.", order.ID)

	log.Printf("Order %s: All payment validations passed for TxID %s.", order.ID, txNotification.TxID)
	return true, nil
}

// Conceptual helper functions (not implemented here, for future use with EVMChains)
// func findChainConfig(evmChains []models.EVMChainConfig, chainNameOrId string) *models.EVMChainConfig { ... }
// func findTokenInChainConfig(chainConfig *models.EVMChainConfig, tokenSymbol string) *models.EVMErc20Token { ... }
