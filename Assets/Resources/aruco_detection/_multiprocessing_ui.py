from multiprocessing import Value
import tkinter as tk


class ButtonUI(tk.Frame):
    def __init__(self, title, root, execute_signal:Value):
        super().__init__(root)
        self.pack()
        root.title(title)
        self._execute_signal = execute_signal

    def construct_button(self, msg, signal_value, font_color='black'):
        new_button = tk.Button(
            self,
            text=msg,
            fg=font_color,
            command=lambda: self._send_execute_command(msg, signal_value))
        new_button.pack()
        return new_button

    def _send_execute_command(self, msg, signal_value):
        self._execute_signal.value = signal_value
