import { useState } from "react";
import { KeyRound } from "lucide-react";
import { toast } from "sonner";
import {
  clearAdminToken,
  getAdminToken,
  getSuggestedAdminToken,
  hasAdminToken,
  setAdminToken,
} from "@/lib/admin-token";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function AdminTokenButton({ onChange }: { onChange?: () => void }) {
  const [open, setOpen] = useState(false);
  const [value, setValue] = useState("");

  function openDialog() {
    setValue(getAdminToken() || getSuggestedAdminToken());
    setOpen(true);
  }

  async function save() {
    setAdminToken(value);
    setOpen(false);
    toast.success(value.trim() ? "Admin token saved for this browser" : "Admin token cleared");
    onChange?.();
  }

  return (
    <>
      <Button type="button" variant="outline" size="sm" onClick={openDialog}>
        <KeyRound />
        Admin access
        {hasAdminToken() ? (
          <span className="ml-1 size-2 rounded-full bg-emerald-500" aria-hidden />
        ) : null}
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Admin access</DialogTitle>
            <DialogDescription>
              Uploads and edits require the shared admin token. Paste the token your team lead
              shared — it is saved in this browser until you clear it.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-2">
            <Label htmlFor="admin-token">Admin token</Label>
            <Input
              id="admin-token"
              type="password"
              autoComplete="off"
              value={value}
              onChange={(e) => setValue(e.target.value)}
              placeholder="Bearer token value"
            />
          </div>

          <div className="flex flex-wrap justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                clearAdminToken();
                setValue("");
                toast.success("Admin token cleared");
                onChange?.();
              }}
            >
              Clear
            </Button>
            <Button type="button" onClick={save}>
              Save
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
