import { useRef, useState } from "react";

// Six boxes for a 6-digit code. Typing moves to the next box, Backspace goes back,
// and pasting "123456" fills all boxes at once.
// onChange(code) is called with the digits typed so far.
// onComplete(code) is called when all 6 digits are filled.
function OtpInput({ onChange, onComplete, disabled }) {
  const [digits, setDigits] = useState(["", "", "", "", "", ""]);
  const boxes = useRef([]); // the 6 <input> elements

  function focusBox(index) {
    if (boxes.current[index]) {
      boxes.current[index].focus();
      boxes.current[index].select();
    }
  }

  function update(newDigits) {
    setDigits(newDigits);
    const code = newDigits.join("");
    onChange(code);
    if (code.length === 6 && onComplete) {
      onComplete(code);
    }
  }

  // Puts several digits (paste or phone autofill) into the boxes, starting at "start".
  function fillFrom(start, text) {
    const newDigits = digits.slice();
    let position = start;
    for (let i = 0; i < text.length && position < 6; i++) {
      newDigits[position] = text.charAt(i);
      position++;
    }
    update(newDigits);
    focusBox(Math.min(position, 5));
  }

  function handleChange(index, event) {
    const typed = event.target.value.replace(/\D/g, ""); // keep only digits

    if (typed.length === 0) {
      const newDigits = digits.slice();
      newDigits[index] = "";
      update(newDigits);
      return;
    }

    if (typed.length > 1) {
      fillFrom(index, typed);
      return;
    }

    const newDigits = digits.slice();
    newDigits[index] = typed;
    update(newDigits);
    if (index < 5) {
      focusBox(index + 1);
    }
  }

  function handleKeyDown(index, event) {
    if (event.key === "Backspace" && digits[index] === "" && index > 0) {
      event.preventDefault();
      const newDigits = digits.slice();
      newDigits[index - 1] = "";
      update(newDigits);
      focusBox(index - 1);
    } else if (event.key === "ArrowLeft" && index > 0) {
      focusBox(index - 1);
    } else if (event.key === "ArrowRight" && index < 5) {
      focusBox(index + 1);
    }
  }

  function handlePaste(event) {
    const pasted = event.clipboardData.getData("text").replace(/\D/g, "");
    if (pasted.length > 0) {
      event.preventDefault();
      fillFrom(0, pasted);
    }
  }

  return (
    <div className="otp-input" onPaste={handlePaste}>
      {digits.map(function (digit, index) {
        return (
          <input
            key={index}
            ref={(element) => (boxes.current[index] = element)}
            className={digit ? "otp-box filled" : "otp-box"}
            type="text"
            inputMode="numeric"
            autoComplete={index === 0 ? "one-time-code" : "off"}
            maxLength={6}
            value={digit}
            disabled={disabled}
            autoFocus={index === 0}
            aria-label={"Digit " + (index + 1)}
            onChange={(event) => handleChange(index, event)}
            onKeyDown={(event) => handleKeyDown(index, event)}
            onFocus={(event) => event.target.select()}
          />
        );
      })}
    </div>
  );
}

export default OtpInput;
