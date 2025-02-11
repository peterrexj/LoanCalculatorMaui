using LoanCalculator.Core.StampDuty.AustralianStates;
using LoanCalculator.Models;
using LoanCalculator.Models.Enums;

namespace LoanCalculator.Core.StampDuty
{
    public class StampDutyCalculator
    {
        private readonly StampDutyNsw _stampDutyNsw = new StampDutyNsw();
        private readonly StampDutyAct _stampDutyAct = new StampDutyAct();
        private readonly StampDutyQld _stampDutyQld = new StampDutyQld();
        private readonly StampDutyVic _stampDutyVic = new StampDutyVic();
        private readonly StampDutySa _stampDutySa = new StampDutySa();
        private readonly StampDutyWa _stampDutyWa = new StampDutyWa();
        private readonly StampDutyTas _stampDutyTas = new StampDutyTas();
        private readonly StampDutyNt _stampDutyNt = new StampDutyNt();

        public StampDutyOutput CalculateStampDutyAustralia(AustralianStatesEnum australianState, double amount)
        {
            if (australianState == AustralianStatesEnum.NSW)
            {
                return  _stampDutyNsw.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.ACT)
            {
                return _stampDutyAct.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.QLD)
            {
                return _stampDutyQld.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.VIC)
            {
                return _stampDutyVic.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.SA)
            {
                return _stampDutySa.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.WA)
            {
                return _stampDutyWa.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.TAS)
            {
                return _stampDutyTas.CalculateCharges(amount).SetState(australianState);
            }
            else if (australianState == AustralianStatesEnum.NT)
            {
                return _stampDutyNt.CalculateCharges(amount).SetState(australianState);
            }
            else
            {
                return new StampDutyOutput().SetState(australianState);
            }
        }
    }
}
