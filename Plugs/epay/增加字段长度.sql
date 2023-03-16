ALTER TABLE `epay`.`pay_plugin` 
MODIFY COLUMN `types` varchar(4096) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL AFTER `link`;