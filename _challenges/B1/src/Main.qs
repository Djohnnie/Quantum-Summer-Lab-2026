// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "B1"
// Title = "Generate |+⟩ state or |-⟩ state"
// Description = "You have to implement an operation which takes a qubit that has been prepared in the |0⟩ state and an integer that specifies the desired sign: +1 for the |+⟩ state and -1 for the |-⟩ state.[BR]You should implement the following Solve operation to make that happen and keep the signature of the operation exactly as it is."
// Tldr = "You should implement the empty Solve operation below and prepare a |+⟩ or |-⟩ quantum state depending on the provided sign value."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// [
//   {
//     "id": "|00⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|01⟩",
//     "amplitudeReal": -0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|10⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|11⟩",
//     "amplitudeReal": -0.5,
//     "amplitudeImaginary": 0
//   }
// ]
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 2
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{    
    let result1 = CheckOperationsAreEqual(1, x => Solve(x[0], 1), x => Expected(x[0], 1));
    LogMessage(result1, "You have successfully generated the |+⟩ state for sign 1", "You have failed to generate the |+⟩ state for sign 1");

    let result2 = CheckOperationsAreEqual(1, x => Solve(x[0], -1), x => Expected(x[0], -1));
    LogMessage(result2, "You have successfully generated the |-⟩ state for sign -1", "You have failed to generate the |-⟩ state for sign -1");

    use (q1, q2) = (Qubit(), Qubit());
    Solve(q1, 1);
    Solve(q2, -1);
    DumpRegister([q1, q2]);
    ResetAll([q1, q2]);

    return result1 and result2;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation Expected (q : Qubit, sign : Int) : Unit is Adj
{
    if(sign < 0)
    {
        X(q);
    }

    H(q);
}

// ===SOLVE-START===
operation Solve (q : Qubit, sign : Int) : Unit
{
    if(sign < 0)
    {
        X(q);
    }

    H(q);
}
// ===SOLVE-END===
