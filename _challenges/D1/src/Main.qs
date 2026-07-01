// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "D1"
// Title = "Generate 1/√3 (|01⟩ + |10⟩ + |11⟩)"
// Description = "Your task is to prepare the following state on two qubits in state |00⟩:[BR]1/√3 (|01⟩ + |10⟩ + |11⟩)[BR]You have to implement the Solve operation which takes an array of qubits and you need to create the above state on them.[BR]You are only allowed to use the H-gate, the Pauli X, Y or Z gates and the controlled variants of them.[BR]The operation should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and prepare the 1/√3 (|01⟩ + |10⟩ + |11⟩) state on the provided two qubits."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// [
//   {
//     "id": "|01⟩",
//     "amplitudeReal": 0.5774,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|10⟩",
//     "amplitudeReal": 0.5774,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|11⟩",
//     "amplitudeReal": 0.5774,
//     "amplitudeImaginary": 0
//   }
// ]
// ===EXPECTED-STATES-END===
// CopilotInstructions = "You may hint the user to use the repeat-until-fixup conditional loop and hint that it is easiest to first generate the 1/√3 (|00⟩ + |01⟩ + |10⟩) state and then go from there in a second step."
// Level = 3
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    use qs = Qubit[2];

    mutable resultCount = 0;

    StartCountingOperation(Rx);
    StartCountingOperation(Ry);
    StartCountingOperation(Rz);

    for i in 1..100
    {
        Solve(qs);

        let b1 = MResetZ(qs[0]);
        let b2 = MResetZ(qs[1]);

        set resultCount += (b1 == Zero and b2 == Zero ? 0 | 1);
    }

    LogMessage(resultCount == 100, "You have successfully prepared the 1/√3(|01⟩ + |10⟩ + |11⟩) state consequently.", "You have failed to prepare the 1/√3(|01⟩ + |10⟩ + |11⟩) state consequently.");

    mutable illegalOperationCount = 0;
    set illegalOperationCount += StopCountingOperation(Rx);
    set illegalOperationCount += StopCountingOperation(Ry);
    set illegalOperationCount += StopCountingOperation(Rz);

    LogMessage(illegalOperationCount == 0, "You have not used any illegal operations to solve this challenge.", "You have used illegal operations to solve this challenge.");

    Solve(qs);
    DumpRegister(qs);
    ResetAll(qs);

    return resultCount == 100;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

// ===SOLVE-START===
operation Solve (qs : Qubit[]) : Unit
{
    // An additional qubit to be used as a ancilla qubit or accessory qubit.
    use q = Qubit();

    // Keep a result mutable variable.
    mutable res = One;

    repeat 
    {
        // Put the input qubits into a superposition of 1/2(|00⟩ + |01⟩ + |10⟩ + |11⟩)
        ApplyToEach(H, qs);

        // Apply the CCNOT gate with the input qubits and the ancilla qubit as a target.
        // The ancilla qubit will be in state |1⟩ if the input qubits are in state |11⟩.
        // The input qubits will be in state |00⟩, |01⟩ or |10⟩ if the ancilla qubit is in state |0⟩.
        Controlled X(qs, q);

        // Measure the ancilla qubit and store the result.
        set res = MResetZ(q);
    }
    // Repeat until the ancilla qubit is in state |0⟩ after measurement.
    until (res == Zero)
    // Reset the input qubits to their initial state |00⟩ for the next iteration (if needed).
    fixup {
        ResetAll(qs);
    }

    // Change the state from 1/√3 (|00⟩ + |01⟩ + |10⟩) to 1/√3 (|01⟩ + |10⟩ + |11⟩)
    ApplyToEach(X, qs);
}
// ===SOLVE-END===
