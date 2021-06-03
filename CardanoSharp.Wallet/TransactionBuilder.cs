﻿using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Models.Transactions;
using PeterO.Cbor2;
using System;
using System.Collections.Generic;
using System.Text;

namespace CardanoSharp.Wallet
{
    public class TransactionBuilder
    {
        private CBORObject _cborTransaction { get; set; }
        private CBORObject _cborTransactionBody { get; set; }
        private CBORObject _cborTransactionInputs { get; set; }
        private CBORObject _cborTransactionOutputs { get; set; }
        private CBORObject _cborTransactionWitnessSet { get; set; }
        private CBORObject _cborVKeyWitnesses { get; set; }

        private void AddInput(TransactionInput transactionInput)
        {
            //create the CBOR Object Array if it hasnt been created yet
            if (_cborTransactionInputs == null) _cborTransactionInputs = CBORObject.NewArray();

            //fill out cbor structure for transaction input
            var cborTransactionInput = CBORObject.NewArray()
                .Add(transactionInput.TransactionId)
                .Add(transactionInput.TransactionIndex);

            //add the new input to the array
            _cborTransactionInputs.Add(cborTransactionInput);
        }

        private void AddOutput(TransactionOutput transactionOutput)
        {
            //create the CBOR Object Array if it hasnt been created yet
            if (_cborTransactionOutputs == null) _cborTransactionOutputs = CBORObject.NewArray();

            //start the cbor transaction output object with the address we are sending
            var cborTransactionOutput = CBORObject.NewArray()
                .Add(transactionOutput.Address);

            //determine if the output has any native assets included
            if(transactionOutput.Value.MultiAsset != null)
            {
                //add any 'coin' aka ADA to the output
                var cborAssetOutput = CBORObject.NewArray()
                    .Add(transactionOutput.Value.Coin);

                //iterate over the multiassets
                //reminder of this structure
                //MultiAsset = Rust Type of BTreeMap<PolicyID, Assets>
                //PolicyID = byte[](length 28)
                //Assets = BTreeMap<AssetName, uint>
                //AssetName = byte[](length 28)
                foreach (var policy in transactionOutput.Value.MultiAsset)
                {
                    //in this scope
                    //policy.Key = PolicyID
                    //policy.Values = Assets

                    var assetMap = CBORObject.NewMap();
                    foreach(var asset in policy.Value.Token)
                    {
                        //in this scope
                        //asset.Key = AssetName
                        //asset.Value = uint
                        assetMap.Add(asset.Key, asset.Value);
                    }

                    //add our PolicyID (policy.Key) and Assets (assetMap)
                    var multiassetMap = CBORObject.NewMap()
                        .Add(policy.Key, assetMap);

                    //add our multiasset to our assetOutput
                    cborAssetOutput.Add(multiassetMap);
                }

                //finally add our assetOutput to our transaction output
                cborTransactionOutput.Add(cborAssetOutput);
            }else
            {
                //heres a simple send ada transaction
                cborTransactionOutput.Add(transactionOutput.Value.Coin);
            }

            _cborTransactionOutputs.Add(cborTransactionOutput);
        }

        private void AddWithdrawals(byte[] key, uint value)
        {
            //key = reward_account
            //value = coin/amount
            //add a new Withdrawal to the TransactionBody
        }

        private void AddCertificates()
        {
            /*
            certificate =
            [stake_registration
            // stake_deregistration
            // stake_delegation
            // pool_registration
            // pool_retirement
            // genesis_key_delegation
            // move_instantaneous_rewards_cert
            ]

            stake_registration = (0, stake_credential)
            stake_deregistration = (1, stake_credential)
            stake_delegation = (2, stake_credential, pool_keyhash)
            pool_registration = (3, pool_params)
            pool_retirement = (4, pool_keyhash, epoch)
            genesis_key_delegation = (5, genesishash, genesis_delegate_hash, vrf_keyhash)
            move_instantaneous_rewards_cert = (6, move_instantaneous_reward)*/
            //Certificates are byte[] but we may need to denote type...
            //add a new Certificate
        }

        private void BuildBody(TransactionBody transactionBody)
        {
            _cborTransactionBody = CBORObject.NewMap();

            //add all the transaction inputs
            foreach (var txInput in transactionBody.TransactionInputs)
            {
                AddInput(txInput);
            }

            if (_cborTransactionInputs != null) _cborTransactionBody.Add(0, _cborTransactionInputs);


            //add all the transaction outputs
            foreach (var txOutput in transactionBody.TransactionOutputs)
            {
                AddOutput(txOutput);
            }

            if (_cborTransactionOutputs != null) _cborTransactionBody.Add(1, _cborTransactionOutputs);

            //add fee
            _cborTransactionBody.Add(2, transactionBody.Fee);

            //add ttl
            if (transactionBody.Ttl.HasValue) _cborTransactionBody.Add(3, transactionBody.Ttl.Value);
        }

        private void AddVKeyWitnesses(VKeyWitness vKeyWitness)
        {
            //create the CBOR Object Array if it hasnt been created yet
            if (_cborVKeyWitnesses == null) _cborVKeyWitnesses = CBORObject.NewArray();

            //fill out cbor structure for vkey witnesses
            var cborVKeyWitness = CBORObject.NewArray()
                .Add(vKeyWitness.VKey)
                .Add(vKeyWitness.Signature);

            //add the new input to the array
            _cborVKeyWitnesses.Add(cborVKeyWitness);
        }

        private void BuildWitnessSet(TransactionWitnessSet transactionWitnessSet)
        {
            _cborTransactionWitnessSet = CBORObject.NewMap();

            foreach(var vkeyWitness in transactionWitnessSet.VKeyWitnesses)
            {
                AddVKeyWitnesses(vkeyWitness);
            }

            if (_cborVKeyWitnesses != null) _cborVKeyWitnesses.Add(0, _cborVKeyWitnesses);
        }

        public byte[] Build(Transaction transaction)
        {
            //create Transaction CBOR Object
            _cborTransaction = CBORObject.NewArray();

            //if we have a transaction body, lets build Body CBOR and add to Transaction Array
            if (transaction.TransactionBody != null)
            {
                BuildBody(transaction.TransactionBody);
                _cborTransaction.Add(_cborTransactionBody);
            }

            //if we have a transaction witness set, lets build Witness Set CBOR and add to Transaction Array
            if (transaction.TransactionWitnessSet != null)
            {
                BuildWitnessSet(transaction.TransactionWitnessSet);
                _cborTransaction.Add(_cborTransactionWitnessSet);
            }

            //return serialized cbor
            return _cborTransaction.EncodeToBytes();
        }

        public long CalculateFee(byte[] transaction)
        {
            return transaction.Length * FeeStructure.Coefficient + FeeStructure.Constant;
        }
    }
}
