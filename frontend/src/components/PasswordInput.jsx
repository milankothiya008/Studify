import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";

// A password box with a button to show or hide what was typed.
function PasswordInput({ value, onChange, minLength, placeholder, autoComplete }) {
  const [visible, setVisible] = useState(false);

  return (
    <div className="input-with-button">
      <input
        type={visible ? "text" : "password"}
        value={value}
        onChange={onChange}
        minLength={minLength}
        placeholder={placeholder}
        autoComplete={autoComplete}
        required
      />
      <button
        type="button"
        className="input-button"
        onClick={() => setVisible(!visible)}
        aria-label={visible ? "Hide password" : "Show password"}
      >
        {visible ? <EyeOff size={18} /> : <Eye size={18} />}
      </button>
    </div>
  );
}

export default PasswordInput;
