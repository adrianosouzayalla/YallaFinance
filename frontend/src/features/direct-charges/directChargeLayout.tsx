import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import PersonSearchOutlinedIcon from "@mui/icons-material/PersonSearchOutlined";
import PlaylistAddOutlinedIcon from "@mui/icons-material/PlaylistAddOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";

export const stepLabels = ["Cliente", "Débitos", "Revisão", "Confirmação"];

export const stepIcons = [
  <PersonSearchOutlinedIcon key="customer" fontSize="small" />,
  <PlaylistAddOutlinedIcon key="debts" fontSize="small" />,
  <FactCheckOutlinedIcon key="review" fontSize="small" />,
  <TaskAltOutlinedIcon key="confirmation" fontSize="small" />,
];

export const debtsPerPage = 3;

// Single form as requested, retaining the existing compact fields and spacing.

// Existing form spacing, with fields filling the available container width.
